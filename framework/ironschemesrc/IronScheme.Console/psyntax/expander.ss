;;; Copyright (c) 2006, 2007  Abdulaziz Ghuloum and Kent Dybvig
;;; Copyright (c) 2008, 2009  Abdulaziz Ghuloum
;;; Copyright (c) 2007, 2008, 2009  Llewellyn Pritchard
;;; Permission is hereby granted, free of charge, to any person obtaining a
;;; copy of this software and associated documentation files (the "Software"),
;;; to deal in the Software without restriction, including without limitation
;;; the rights to use, copy, modify, merge, publish, distribute, sublicense,
;;; and/or sell copies of the Software, and to permit persons to whom the
;;; Software is furnished to do so, subject to the following conditions:
;;; 
;;; The above copyright notice and this permission notice shall be included in
;;; all copies or substantial portions of the Software.
;;; 
;;; THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
;;; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
;;; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
;;; THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
;;; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
;;; FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
;;; DEALINGS IN THE SOFTWARE. 

(library (psyntax expander)
  (export identifier? syntax-dispatch 
          eval core-expand 
          generate-temporaries free-identifier=?
          bound-identifier=? datum->syntax syntax-error
          syntax-violation
          syntax->datum 
          make-variable-transformer make-compile-time-value
          pre-compile-r6rs-top-level
          variable-transformer? parse-top-level-program
          variable-transformer-procedure core-library-expander parse-library
          compile-r6rs-top-level boot-library-expand top-level-expander
          null-environment scheme-report-environment
          interaction-environment
          expand->core
          new-interaction-environment
          interaction-environment-symbols environment-bindings
          ellipsis-map assertion-error syntax-transpose
		      environment environment? environment-symbols)
  (import
    (except (rnrs)
      environment environment? identifier?
      eval generate-temporaries free-identifier=?
      bound-identifier=? datum->syntax syntax-error
      syntax-violation syntax->datum make-variable-transformer
      null-environment scheme-report-environment)
    (only (ironscheme) valuetype-vector?)  
    (rnrs mutable-pairs)
    (psyntax library-manager)
    (psyntax builders)
    (psyntax compat)
    ;(psyntax config)
    (psyntax internal)
    (only (rnrs syntax-case) syntax-case syntax with-syntax)
    (prefix (rnrs syntax-case) sys.))
  
  (define (set-cons x ls)
    (cond
      ((memq x ls) ls)
      (else (cons x ls))))
 
  (define (set-union ls1 ls2)
    (cond
      ((null? ls1) ls2)
      ((memq (car ls1) ls2) (set-union (cdr ls1) ls2))
      (else (cons (car ls1) (set-union (cdr ls1) ls2)))))
  
  (define-syntax no-source 
    (lambda (x) #f))

  ;;; the body of a library, when it's first processed, gets this
  ;;; set of marks. 
  (define top-mark* '(top))

  ;;; consequently, every syntax object that has a top in its marks 
  ;;; set was present in the program source.
  (define top-marked?
    (lambda (m*) (memq 'top m*)))
  
  ;;; This procedure generates a fresh lexical name for renaming.
  ;;; It's also use it to generate temporaries.
  (define gen-lexical
    (lambda (sym)
      (cond
        ((symbol? sym) (gensym sym))
        ((stx? sym) (gen-lexical (id->sym sym)))
        (else (assertion-violation 'gen-lexical "BUG: invalid arg" sym)))))
  
  ;;; gen-global is used to generate global names (e.g. locations
  ;;; for library exports).  We use gen-lexical since it works just 
  ;;; fine.
  (define (gen-global x) (gen-lexical x))

  ;;; every identifier in the program would have a label associated
  ;;; with it in its substitution.  gen-label generates such labels.
  ;;; the labels have to have read/write eq? invariance to support 
  ;;; separate compilation.
  (define gen-label
    (lambda (_) (gensym)))

  (define (gen-top-level-label id rib)
    (define (find sym mark* sym* mark** label*)
      (and (pair? sym*)
           (if (and (eq? sym (car sym*)) (same-marks? mark* (car mark**)))
               (car label*)
               (find sym mark* (cdr sym*) (cdr mark**) (cdr label*)))))
    (let ((sym (id->sym id))
          (mark* (stx-mark* id)))
      (let ((sym* (rib-sym* rib)))
        (cond
          ((and (memq sym (rib-sym* rib))
                (find sym mark* sym* (rib-mark** rib) (rib-label* rib)))
           =>
           (lambda (label)
             (cond
               ((imported-label->binding label) 
                ;;; create new label to shadow imported binding
                (gensym))
               (else
                ;;; recycle old label
                label))))
          (else 
           ;;; create new label for new binding
           (gensym))))))

  (define (gen-define-label+loc id rib sd?)
    (if sd?
        (values (gensym) (gen-lexical id))
        (let ([env (top-level-context)])
          (let ((label (gen-top-level-label id rib))
                (locs (interaction-env-locs env)))
            (values label
              (cond
                ((assq label locs) => cdr)
                (else 
                 (let ((loc (gen-lexical id)))
                   (set-interaction-env-locs! env
                     (cons (cons label loc) locs))
                   loc))))))))

  (define (gen-define-label id rib sd?)
    (if sd?
        (gensym)
        (gen-top-level-label id rib)))


  ;;; A rib is a record constructed at every lexical contour in the
  ;;; program to hold information about the variables introduced in that
  ;;; contour.  Adding an identifier->label mapping to an extensible rib
  ;;; is achieved by consing the identifier's name to the list of
  ;;; symbols, consing the identifier's list of marks to the rib's
  ;;; mark**, and consing the label to the rib's labels.

  (define-record rib (sym* mark** label* sealed/freq cache))
 
  (define make-empty-rib
    (lambda ()
      (make-rib '() '() '() #f #f)))

  (define (top-marked-symbols rib)
    (let-values ([(sym* mark**) 
                  (let ([sym* (rib-sym* rib)] [mark** (rib-mark** rib)])
                    (if (rib-sealed/freq rib)
                        (values (vector->list sym*) (vector->list mark**))
                        (values sym* mark**)))])
      (let f ([sym* sym*] [mark** mark**])
        (cond
          [(null? sym*) '()]
          [(equal? (car mark**) top-mark*) 
           (cons (car sym*) (f (cdr sym*) (cdr mark**)))]
          [else (f (cdr sym*) (cdr mark**))]))))
      
  (define make-cache-rib
    (lambda ()
      (make-rib '() '() '() #f (make-eq-hashtable)))) 
      
  (define (find-label rib sym mark*)
    (let ((ht (rib-cache rib)))
      (and ht
        (let ((cv (hashtable-ref ht sym #f)))
          (cond
            ((and cv (assp (lambda (m) (same-marks? mark* m)) cv)) => cdr)
            (else #f))))))
  
  ;;; For example, when processing a lambda's internal define, a new rib
  ;;; is created and is added to the body of the lambda expression.
  ;;; When an internal definition is encountered, a new entry for the
  ;;; identifier is added (via side effect) to the rib.  A rib may be
  ;;; extensible, or sealed.  An extensible rib looks like:
  ;;;  #<rib list-of-symbols list-of-list-of-marks list-of-labels #f>
  
  (define (extend-rib! rib id label sd?)
    (define (find sym mark* sym* mark** label*)
      (and (pair? sym*)
           (if (and (eq? sym (car sym*)) (same-marks? mark* (car mark**)))
               label*
               (find sym mark* (cdr sym*) (cdr mark**) (cdr label*)))))
    (when (rib-sealed/freq rib)
      (assertion-violation 'extend-rib! "BUG: rib is sealed" rib))
    (let ((sym (id->sym id))
          (mark* (stx-mark* id)))
      (let ((sym* (rib-sym* rib)))
        (cond
          ((find-label rib sym mark*)
           =>
           (lambda (p)
              (unless (eq? label p)
                (stx-error id "multiple definitions of identifier"))))
          ((and (memq sym (rib-sym* rib))
                (find sym mark* sym* (rib-mark** rib) (rib-label* rib)))
           =>
           (lambda (p)
             (unless (eq? label (car p))
               (cond
                 ((not sd?) ;(top-level-context)
                  ;;; XXX override label
                  (set-car! p label))
                 (else
                  ;;; signal an error if the identifier was already
                  ;;; in the rib.
     			        (stx-error id "multiple definitions of identifier"))))))
          (else
           (when (rib-cache rib)
              (hashtable-update! (rib-cache rib) sym 
                (lambda (e)
                  (cons (cons mark* label) e))
                '()))
           (set-rib-sym*! rib (cons sym sym*))
           (set-rib-mark**! rib (cons mark* (rib-mark** rib)))
      	   (set-rib-label*! rib (cons label (rib-label* rib))))))))
          
  (define (extend-rib/nc! rib id label)
    (let ((sym (id->sym id))
          (mark* (stx-mark* id)))
      (let ((sym* (rib-sym* rib)))
       (when (rib-cache rib)
          (hashtable-update! (rib-cache rib) sym 
            (lambda (e)
              (cons (cons mark* label) e))
            '()))      
       (set-rib-sym*! rib (cons sym sym*))
       (set-rib-mark**! rib (cons mark* (rib-mark** rib)))
       (set-rib-label*! rib (cons label (rib-label* rib))))))          


  ;;; A rib can be sealed once all bindings are inserted.  To seal
  ;;; a rib, we convert the lists sym*, mark**, and label* to vectors 
  ;;; and insert a frequency vector in the sealed/freq field.
  ;;; The frequency vector is an optimization that allows the rib to
  ;;; reorganize itself by bubbling frequently used mappings to the 
  ;;; top of the rib.  The vector is maintained in non-descending
  ;;; order and an identifier's entry in the rib is incremented at
  ;;; every access.  If an identifier's frequency exceeds the
  ;;; preceeding one, the identifier's position is promoted to the
  ;;; top of its class (or the bottom of the previous class).
  
  (define (make-rib-map sym*)
    (let ((ht (make-eq-hashtable)))
      (let f ((i 0)(sym* sym*))
        (if (null? sym*) 
          ht
          (begin
            (hashtable-update! ht (car sym*) 
              (lambda (x) (cons i x)) '())
            (f (+ i 1) (cdr sym*)))))))
  
  (define (check/size? lst)
    (let f ((lst lst)(i 0))
      (cond
        [(fx>? i 16) #f]
        [(null? lst) #t]
        [else 
          (f (cdr lst) (fx+ i 1))])))

  (define (seal-rib! rib)
    (let ((sym* (rib-sym* rib)))
      (unless (check/size? sym*)
        ;;; only seal if rib is not empty.
        (set-rib-sym*! rib (list->vector sym*))
        (set-rib-mark**! rib
          (list->vector (rib-mark** rib)))
        (set-rib-label*! rib
          (list->vector (rib-label* rib)))
        (set-rib-sealed/freq! rib
          (make-rib-map sym*)))))

  (define (unseal-rib! rib)
    (when (rib-sealed/freq rib)
      (set-rib-sealed/freq! rib #f)
      (set-rib-sym*! rib (vector->list (rib-sym* rib)))
      (set-rib-mark**! rib (vector->list (rib-mark** rib)))
      (set-rib-label*! rib (vector->list (rib-label* rib)))))

  #;(define (increment-rib-frequency! rib idx)
    (let ((freq* (rib-sealed/freq rib)))
      (let ((freq (vector-ref freq* idx)))
        (let ((i
               (let f ((i idx))
                 (cond
                   ((zero? i) 0)
                   (else
                    (let ((j (- i 1)))
                      (cond
                        ((= freq (vector-ref freq* j)) (f j))
                        (else i))))))))
          (vector-set! freq* i (+ freq 1))
          (unless (= i idx)
            (let ((sym* (rib-sym* rib))
                  (mark** (rib-mark** rib))
                  (label* (rib-label* rib)))
              (let ((sym (vector-ref sym* idx)))
                (vector-set! sym* idx (vector-ref sym* i))
                (vector-set! sym* i sym))
              (let ((mark* (vector-ref mark** idx)))
                (vector-set! mark** idx (vector-ref mark** i))
                (vector-set! mark** i mark*))
              (let ((label (vector-ref label* idx)))
                (vector-set! label* idx (vector-ref label* i))
                (vector-set! label* i label))))))))

  (define make-full-rib ;;; it may be a good idea to seal this rib
    (lambda (id* label*)
      (let ((r (make-rib (map id->sym id*) (map stx-mark* id*) label* #f #f)))
        (seal-rib! r)
        r)))

  ;;; Now to syntax objects which are records defined like:
  (define-record stx (expr mark* subst* ae*)
    (lambda (x p wr)
      (display "#<syntax " p)
      (write (stx->datum x) p)
      (let ((expr (stx-expr x)))
        (when (annotation? expr) 
          (let ((src (annotation-source expr)))
            (when (pair? src)
              (display " (" p)
              (display (cdr src) p)
              (display " of " p)
              (display (car src) p)
              (display ")" p)))))
      (display ">" p)))

  ;;; First, let's look at identifiers, since they're the real 
  ;;; reason why syntax objects are here to begin with.
  ;;; An identifier is an stx whose expr is a symbol.
  ;;; In addition to the symbol naming the identifier, the identifer
  ;;; has a list of marks and a list of substitutions.
  ;;; The idea is that to get the label of an identifier, we look up
  ;;; the identifier's substitutions for a mapping with the same
  ;;; name and same marks (see same-marks? below).

  ;;; Since all the identifier->label bindings are encapsulated
  ;;; within the identifier, converting a datum to a syntax object
  ;;; (non-hygienically) is done simply by creating an stx that has
  ;;; the same marks and substitutions as the identifier.
  (define datum->stx
    (lambda (id datum)
      (make-stx datum (stx-mark* id) (stx-subst* id) (stx-ae* id))))

  ;;; A syntax object may be wrapped or unwrapped, so what does that
  ;;; mean exactly?  
  ;;;
  ;;; A wrapped syntax object is just a way of saying it's an stx 
  ;;; record.  All identifiers are stx records (with a symbol in
  ;;; their expr field).  Other objects such as pairs and vectors 
  ;;; may be wrapped or unwrapped.  A wrapped pair is an stx whos
  ;;; expr is a pair.  An unwrapped pair is a pair whos car and cdr
  ;;; fields are themselves syntax objects (wrapped or unwrapped).
  ;;;
  ;;; We always maintain the invariant that we don't double wrap 
  ;;; syntax objects.  The only way to get a doubly-wrapped syntax
  ;;; object is by doing datum->stx (above) where the datum is
  ;;; itself a wrapped syntax object (r6rs may not even consider 
  ;;; wrapped syntax objects as datum, but let's not worry now).

  ;;; Syntax objects have, in addition to the expr, a 
  ;;; substitution field (stx-subst*).  The subst* is a list
  ;;; where each element is either a rib or the symbol "shift".
  ;;; Normally, a new rib is added to an stx at evert lexical
  ;;; contour of the program in order to capture the bindings
  ;;; inctroduced in that contour.  

  ;;; The mark* field of an stx is, well, a list of marks.
  ;;; Each of these marks can be either a generated mark 
  ;;; or an antimark.
  ;;; (two marks must be eq?-comparable, so we use a string
  ;;; of one char (this assumes that strings are mutable)).
  
  ;;; gen-mark generates a new unique mark
  (define (gen-mark) ;;; faster
    (string #\m))
  
  ;(define gen-mark ;;; useful for debugging
  ;  (let ((i 0))
  ;    (lambda () 
  ;      (set! i (+ i 1))
  ;      (string-append "m." (number->string i)))))
  
  ;;; We use #f as the anti-mark.
  (define anti-mark #f)
  (define anti-mark? not)
  
  ;;; So, what's an anti-mark and why is it there.
  ;;; The theory goes like this: when a macro call is encountered, 
  ;;; the input stx to the macro transformer gets an extra anti-mark,
  ;;; and the output of the transformer gets a fresh mark.
  ;;; When a mark collides with an anti-mark, they cancel one
  ;;; another.  Therefore, any part of the input transformer that
  ;;; gets copied to the output would have a mark followed
  ;;; immediately by an anti-mark, resulting in the same syntax
  ;;; object (no extra marks).  Parts of the output that were not
  ;;; present in the input (e.g. inserted by the macro transformer) 
  ;;; would have no anti-mark and, therefore, the mark would stick 
  ;;; to them.
  ;;; 
  ;;; Every time a mark is pushed to an stx-mark* list, a
  ;;; corresponding 'shift is pushed to the stx-subst* list.
  ;;; Every time a mark is cancelled by an anti-mark, the
  ;;; corresponding shifts are also cancelled.  

  ;;; The procedure join-wraps, here, is used to compute the new
  ;;; mark* and subst* that would result when the m1* and s1* are 
  ;;; added to an stx's mark* and subst*.
  ;;; The only tricky part here is that e may have an anti-mark 
  ;;; that should cancel with the last mark in m1*.
  ;;; So, if m1* is (mx* ... mx)
  ;;;    and m2* is (#f my* ...) 
  ;;; then the resulting marks should be (mx* ... my* ...)
  ;;; since mx would cancel with the anti-mark.
  ;;; The substs would have to also cancel since 
  ;;;     s1* is (sx* ... sx)
  ;;; and s2* is (sy sy* ...) 
  ;;; then the resulting substs should be (sx* ... sy* ...)
  ;;; Notice that both sx and sy would be shift marks.
  (define join-wraps
    (lambda (m1* s1* ae1* e)
      (define merge-ae*
        (lambda (ls1 ls2)
          (if (and (pair? ls1) (pair? ls2) (not (car ls2)))
              (cancel ls1 ls2)
              (append ls1 ls2))))
      (define cancel
        (lambda (ls1 ls2)
          (let f ((x (car ls1)) (ls1 (cdr ls1)))
            (if (null? ls1)
                (cdr ls2)
                (cons x (f (car ls1) (cdr ls1)))))))
      (let ((m2* (stx-mark* e)) 
            (s2* (stx-subst* e))
            (ae2* (stx-ae* e)))
        (if (and (not (null? m1*))
                 (not (null? m2*))
                 (anti-mark? (car m2*)))
            ; cancel mark, anti-mark, and corresponding shifts
            (values (cancel m1* m2*) (cancel s1* s2*) (merge-ae* ae1* ae2*))
            (values (append m1* m2*) (append s1* s2*) (merge-ae* ae1* ae2*))))))

  ;;; The procedure mkstx is then the proper constructor for
  ;;; wrapped syntax objects.  It takes a syntax object, a list
  ;;; of marks, and a list of substs.  It joins the two wraps 
  ;;; making sure that marks and anti-marks and corresponding 
  ;;; shifts cancel properly.
  (define mkstx 
    (lambda (e m* s* ae*)
      (if (and (stx? e) (not (top-marked? m*)))
          (let-values (((m* s* ae*) (join-wraps m* s* ae* e)))
            (make-stx (stx-expr e) m* s* ae*))
          (make-stx e m* s* ae*))))
  
  (define add-subst
    (lambda (subst e)
      (mkstx e '() (list subst) '())))

  (define add-mark
    (lambda (mark subst expr ae)
      (define merge-ae*
        (lambda (ls1 ls2)
          (if (and (pair? ls1) (pair? ls2) (not (car ls2)))
              (cancel ls1 ls2)
              (append ls1 ls2))))
      (define cancel
        (lambda (ls1 ls2)
          (let f ((x (car ls1)) (ls1 (cdr ls1)))
            (if (null? ls1)
                (cdr ls2)
                (cons x (f (car ls1) (cdr ls1)))))))
      (define (f e m s1* ae*)
        (cond
          [(pair? e)
           (let ([a (f (car e) m s1* ae*)] 
                 [d (f (cdr e) m s1* ae*)])
             (if (eq? a d) e (cons a d)))]
          [(vector? e) 
           (let ([ls1 (vector->list e)])
             (let ([ls2 (map (lambda (x) (f x m s1* ae*)) ls1)])
               (if (for-all eq? ls1 ls2) e (list->vector ls2))))]
          [(stx? e)
           (let ([m* (stx-mark* e)] [s2* (stx-subst* e)])
             (cond
               [(null? m*)
                (f (stx-expr e) m 
                   (append s1* s2*)
                   (merge-ae* ae* (stx-ae* e)))]
               [(eq? (car m*) anti-mark)
                (make-stx (stx-expr e) (cdr m*)
                  (cdr (append s1* s2*))
                  (merge-ae* ae* (stx-ae* e)))]
               [else
                (make-stx (stx-expr e)
                  (cons m m*)
                  (let ([s* (cons 'shift (append s1* s2*))])
                    (if subst (cons subst s*) s*))
                  (merge-ae* ae* (stx-ae* e)))]))]
          [(symbol? e)
           (syntax-violation #f 
               "raw symbol encountered in output of macro"
               expr e)]
          [else (make-stx e (list m) s1* ae*)]))
      (mkstx (f expr mark '() '()) '() '() (list ae))))

  ;;; now are some deconstructors and predicates for syntax objects.
  (define syntax-kind?
    (lambda (x p?)
      (cond
        ((stx? x) (syntax-kind? (stx-expr x) p?))
        ((annotation? x) 
         (syntax-kind? (annotation-expression x) p?))
        (else (p? x)))))

  (define syntax-vector->list
    (lambda (x)
      (cond
        ((stx? x)
         (let ((ls (syntax-vector->list (stx-expr x)))
               (m* (stx-mark* x))
               (s* (stx-subst* x))
               (ae* (stx-ae* x)))
           (map (lambda (x) (mkstx x m* s* ae*)) ls)))
        ((annotation? x) 
         (syntax-vector->list (annotation-expression x)))
        ((vector? x) (vector->list x))
        (else (assertion-violation 'syntax-vector->list "BUG: not a syntax vector" x)))))
  (define syntax-pair?
    (lambda (x) (syntax-kind? x pair?)))
  (define syntax-vector?
    (lambda (x) (syntax-kind? x vector?)))
  (define syntax-null?
    (lambda (x) (syntax-kind? x null?)))
  (define syntax-list? ;;; FIXME: should terminate on cyclic input.
    (lambda (x)
      (or (syntax-null? x)
          (and (syntax-pair? x) (syntax-list? (syntax-cdr x))))))
  (define syntax-car
    (lambda (x)
      (cond
        ((stx? x)
         (mkstx (syntax-car (stx-expr x)) 
                (stx-mark* x) 
                (stx-subst* x)
                (stx-ae* x)))
        ((annotation? x) 
         (syntax-car (annotation-expression x)))
        ((pair? x) (car x))
        (else (assertion-violation 'syntax-car "BUG: not a pair" x)))))
  (define syntax-cdr
    (lambda (x)
      (cond
        ((stx? x)
         (mkstx (syntax-cdr (stx-expr x)) 
                (stx-mark* x)
                (stx-subst* x)
                (stx-ae* x)))
        ((annotation? x) 
         (syntax-cdr (annotation-expression x)))
        ((pair? x) (cdr x))
        (else (assertion-violation 'syntax-cdr "BUG: not a pair" x)))))
  (define syntax->list
    (lambda (x)
      (if (syntax-pair? x)
          (cons (syntax-car x) (syntax->list (syntax-cdr x)))
          (if (syntax-null? x)
              '()
              (assertion-violation 'syntax->list "BUG: invalid argument" x)))))

  (define id?
    (lambda (x) 
      (and (stx? x) 
        (let ((expr (stx-expr x)))
          (symbol? (if (annotation? expr)
                       (annotation-stripped expr)
                       expr))))))
  
  (define id->sym
    (lambda (x)
      (unless (stx? x)
        (error 'id->sym "BUG in ikarus: not an id" x))
      (let ((expr (stx-expr x)))
        (let ((sym (if (annotation? expr) 
                       (annotation-stripped expr)
                       expr)))
          (if (symbol? sym) 
              sym
              (error 'id->sym "BUG in ikarus: not an id" x))))))

  ;;; Two lists of marks are considered the same if they have the 
  ;;; same length and the corresponding marks on each are eq?.
  (define same-marks?
    (lambda (x y)
      (or (and (null? x) (null? y)) ;(eq? x y)
          (and (pair? x) (pair? y)
               (eq? (car x) (car y))
               (same-marks? (cdr x) (cdr y))))))
  
  ;;; Two identifiers are bound-id=? if they have the same name and
  ;;; the same set of marks.
  (define bound-id=?
    (lambda (x y)
      (and (eq? (id->sym x) (id->sym y))
           (same-marks? (stx-mark* x) (stx-mark* y)))))

  ;;; Two identifiers are free-id=? if either both are bound to the
  ;;; same label or if both are unbound and they have the same name.
  (define free-id=?
    (lambda (i j)
      (let ((t0 (id->label i)) (t1 (id->label j)))
        (if (or t0 t1)
            (eq? t0 t1)
            (eq? (id->sym i) (id->sym j))))))

  ;;; valid-bound-ids? takes checks if a list is made of identifers
  ;;; none of which is bound-id=? to another.
  (define valid-bound-ids?
    (lambda (id*)
      (and (for-all id? id*)
           (distinct-bound-ids? id*))))

  (define distinct-bound-ids?
    (lambda (id*)
      (or (null? id*)
          (and (not (bound-id-member? (car id*) (cdr id*)))
               (distinct-bound-ids? (cdr id*))))))

  (define bound-id-member?
    (lambda (id id*)
      (and (pair? id*)
           (or (bound-id=? id (car id*))
               (bound-id-member? id (cdr id*))))))

  (define self-evaluating?
    (lambda (x) ;;; am I missing something here?
      (or (number? x) (string? x) (char? x) (boolean? x)
          (valuetype-vector? x))))

  ;;; strip is used to remove the wrap of a syntax object.
  ;;; It takes an stx's expr and marks.  If the marks contain
  ;;; a top-mark, then the expr is returned.  

  (define (strip-annotations x)
    (cond
      ((pair? x) 
       (cons (strip-annotations (car x))
             (strip-annotations (cdr x))))
      ((vector? x) (vector-map strip-annotations x))
      ((annotation? x) (annotation-stripped x))
      (else x)))

  (define strip
    (lambda (x m*)
      (if (top-marked? m*)
          (if (or (annotation? x)
                  (and (pair? x) 
                       (annotation? (car x)))
                  (and (vector? x) (> (vector-length x) 0)
                       (annotation? (vector-ref x 0))))
              ;;; TODO: Ask Kent why this is a sufficient test
              (strip-annotations x)
              x)
          (let f ((x x))
            (cond
              ((stx? x) (strip (stx-expr x) (stx-mark* x)))
              ((annotation? x) (annotation-stripped x))
              ((pair? x)
               (let ((a (f (car x))) (d (f (cdr x))))
                 (if (and (eq? a (car x)) (eq? d (cdr x)))
                     x
                     (cons a d))))
              ((vector? x)
               (let ((old (vector->list x)))
                 (let ((new (map f old)))
                   (if (for-all eq? old new)
                       x
                       (list->vector new)))))
              (else x))))))

  (define stx->datum
    (lambda (x)
      (strip x '())))
      
  (define (same-marks*? mark* mark** si)
    (if (null? si) 
      #f
      (if (same-marks? mark* (vector-ref mark** (car si))) 
        (car si)
        (same-marks*? mark* mark** (cdr si)))))    

  ;;; id->label takes an id (that's a sym x marks x substs) and
  ;;; searches the substs for a label associated with the same sym
  ;;; and marks.
  (define (id->label/intern id)
    (or (id->label id)
        (cond
          ((top-level-context) =>
           (lambda (env)
             ;;; fabricate binding
             (let ((rib (interaction-env-rib env)))
               (let-values (((lab _loc) (gen-define-label+loc id rib #f)))
                 (extend-rib! rib id lab #t) ;;; FIXME
                 lab))))
          (else #f))))
          
  (define id->label
    (lambda (id)
      (let ((sym (id->sym id)))
        (let search ((subst* (stx-subst* id)) (mark* (stx-mark* id)))
          (cond
            ((null? subst*) #f)
            ((eq? (car subst*) 'shift) 
             ;;; a shift is inserted when a mark is added.
             ;;; so, we search the rest of the substitution
             ;;; without the mark.
             (search (cdr subst*) (cdr mark*)))
            (else
             (let ((rib (car subst*)))
               (cond
                 ((rib-sealed/freq rib) =>
                  (lambda (ht)
                    (let ((si (hashtable-ref ht sym #f)))
                      (let ((i (and si 
                            (same-marks*? mark* 
                              (rib-mark** rib) (reverse si)))))
                        (if i
                          (vector-ref (rib-label* rib) i)
                          (search (cdr subst*) mark*))))))
                 ((find-label rib sym mark*))
                 (else
                  (let f ((sym* (rib-sym* rib))
                          (mark** (rib-mark** rib))
                          (label* (rib-label* rib)))
                    (cond
                      ((null? sym*) (search (cdr subst*) mark*))
                      ((and (eq? (car sym*) sym)
                            (same-marks? (car mark**) mark*))
                       (car label*))
                      (else (f (cdr sym*) (cdr mark**) (cdr label*))))))))))))))
  
  ;;; label->binding looks up the label in the environment r as 
  ;;; well as in the global environment.  Since all labels are
  ;;; unique, it doesn't matter which environment we consult first.
  ;;; we lookup the global environment first because it's faster
  ;;; (uses a hash table) while the lexical environment is an alist.
  ;;; If we don't find the binding of a label, we return the binding
  ;;; (displaced-lexical . #f) to indicate such.

  (define label->binding-no-fluids
    (lambda (x r)
      (cond
        ((not x) '(displaced-lexical))
        ((imported-label->binding x) =>
         (lambda (b) 
           (cond
             ((and (pair? b) (eq? (car b) '$core-rtd))
              (cons '$rtd (map bless (cdr b))))
             ((and (pair? b) (eq? (car b) 'global-rtd))
              (let ((lib (cadr b)) (loc (cddr b)))
                (cons '$rtd (symbol-value loc))))
             (else b))))
        ((assq x r) => cdr)
        ((top-level-context) =>
         (lambda (env)
           (cond
             ((assq x (interaction-env-locs env)) =>
              (lambda (p)  ;;; fabricate
                (cons* 'lexical (cdr p) #f)))
             (else '(displaced-lexical . #f)))))
        (else '(displaced-lexical . #f)))))

  (define label->binding
    (lambda (x r)
      (let ([b (label->binding-no-fluids x r)])
        (if (and (pair? b) (eq? (car b) '$fluid))
            ;;; fluids require reversed logic.  We have to look them
            ;;; up in the local environment first before the global.
            (let ([x (cdr b)])
              (cond
                [(assq x r) => cdr]
                [else (label->binding-no-fluids x '())]))
            b))))

  (define make-binding cons)
  (define binding-type car)
  (define binding-value cdr)
  
  ;;; the type of an expression is determined by two things:
  ;;; - the shape of the expression (identifier, pair, or datum)
  ;;; - the binding of the identifier (for id-stx) or the type of
  ;;;   car of the pair.
  (define (raise-unbound-error id) 
    (syntax-violation* #f "unbound identifier" id 
      (make-undefined-violation)))
  (define syntax-type
    (lambda (e r)
      (cond
        ((id? e)
         (let ((id e))
           (let* ((label (id->label/intern id))
                  (b (label->binding label r))
                  (type (binding-type b)))
             (unless label ;;; fail early.
               (raise-unbound-error id))
             (case type
               ((lexical core-prim macro macro! global local-macro
                 local-macro! global-macro global-macro!
                 displaced-lexical syntax import export $module
                 $core-rtd library mutable ctv local-ctv global-ctv)
                (values type (binding-value b) id))
               (else (values 'other #f #f))))))
        ((syntax-pair? e)
         (let ((id (syntax-car e)))
           (if (id? id)
               (let* ((label (id->label/intern id))
                      (b (label->binding label r))
                      (type (binding-type b)))
                 (unless label ;;; fail early.
                   (raise-unbound-error id))
                 (case type
                   ((define define-syntax core-macro begin macro
                      macro! local-macro local-macro! global-macro
                      global-macro! module library set! let-syntax 
                      letrec-syntax import export $core-rtd 
                      ctv local-ctv global-ctv stale-when
                      define-fluid-syntax)
                    (values type (binding-value b) id))
                   (else
                    (values 'call #f #f))))
               (values 'call #f #f))))
        (else (let ((d (stx->datum e)))
                (if (self-evaluating? d)
                    (values 'constant d #f)
                    (values 'other #f #f)))))))

  (define-syntax stx-error
    (lambda (x)
      (syntax-case x ()
        ((_ stx)
         (syntax (syntax-violation #f "invalid syntax" stx)))
        ((_ stx msg)
         (syntax (syntax-violation #f msg stx))))))
  
  ;;; when the rhs of a syntax definition is evaluated, it should be
  ;;; either a procedure, an identifier-syntax transformer or an
  ;;; ($rtd . #<rtd>) form (ikarus/chez).  sanitize-binding converts
  ;;; the output to one of:
  ;;;   (local-macro . procedure)
  ;;;   (local-macro! . procedure)
  ;;;   (local-ctv . compile-time-value)
  ;;;   ($rtd . $rtd)
  ;;; and signals an assertion-violation otherwise.
  (define sanitize-binding
    (lambda (x src)
      (cond
        ((procedure? x)
         (cons* 'local-macro x src))
        ((and (pair? x) (eq? (car x) 'macro!) (procedure? (cdr x)))
         (cons* 'local-macro! (cdr x) src))
        ((and (pair? x) (eq? (car x) '$rtd)) x)
        ((and (pair? x) (eq? (car x) 'ctv)) 
         (cons* 'local-ctv (cdr x) src))
        (else (assertion-violation 'expand "invalid transformer" x)))))
  
  ;;; r6rs's make-variable-transformer:
  (define make-variable-transformer
    (lambda (x)
      (if (procedure? x)
          (cons 'macro! x)
          (assertion-violation 'make-variable-transformer
                 "not a procedure" x))))

  (define make-compile-time-value
    (lambda (x)
      (cons 'ctv x)))

  (define (variable-transformer? x)
    (and (pair? x) (eq? (car x) 'macro!) (procedure? (cdr x))))

  (define (variable-transformer-procedure x)
    (if (variable-transformer? x)
        (cdr x)
        (assertion-violation 
           'variable-transformer-procedure
           "not a variable transformer" 
           x)))

  ;;; make-eval-transformer takes an expanded expression, 
  ;;; evaluates it and returns a proper syntactic binding
  ;;; for the resulting object.
  (define make-eval-transformer
    (lambda (x)
      (sanitize-binding (eval-core (expanded->core x)) x)))

  ;;; The syntax-match macro is almost like syntax-case macro.
  ;;; Except that:
  ;;;   The syntax objects matched are OUR stx objects, not
  ;;;     the host systems syntax objects (whatever they may be
  ;;;     we don't care).
  ;;;   The literals are matched against those in the system 
  ;;;     library (psyntax system $all).   -- see scheme-stx
  ;;;   The variables in the patters are bound to ordinary variables
  ;;;     not to special pattern variables.
  (define-syntax syntax-match
    (lambda (ctx)
      (define convert-pattern
         ; returns syntax-dispatch pattern & ids
          (lambda (pattern keys)
            (define cvt*
              (lambda (p* n ids)
                (if (null? p*)
                    (values '() ids)
                    (let-values (((y ids) (cvt* (cdr p*) n ids)))
                      (let-values (((x ids) (cvt (car p*) n ids)))
                        (values (cons x y) ids))))))
            (define free-identifier-member?
              (lambda (x ls)
                (and (exists (lambda (y) (sys.free-identifier=? x y)) ls) #t)))
            (define (bound-id-member? x ls)
              (and (pair? ls)
                   (or (sys.bound-identifier=? x (car ls))
                       (bound-id-member? x (cdr ls)))))
            (define ellipsis?
              (lambda (x)
                (and (sys.identifier? x)
                     (sys.free-identifier=? x (syntax (... ...))))))
            (define cvt
              (lambda (p n ids)
                (syntax-case p ()
                  (id (sys.identifier? (syntax id))
                   (cond
                     ((bound-id-member? p keys)
                      (values `#(scheme-id ,(sys.syntax->datum p)) ids))
                     ((sys.free-identifier=? p (syntax _))
                      (values '_ ids))
                     (else (values 'any (cons (cons p n) ids)))))
                  ((p dots) (ellipsis? (syntax dots))
                   (let-values (((p ids) (cvt (syntax p) (+ n 1) ids)))
                     (values
                       (if (eq? p 'any) 'each-any `#(each ,p))
                       ids)))
                  ((x dots ys ... . z) (ellipsis? (syntax dots))
                   (let-values (((z ids) (cvt (syntax z) n ids)))
                     (let-values (((ys ids) (cvt* (syntax (ys ...)) n ids)))
                       (let-values (((x ids) (cvt (syntax x) (+ n 1) ids)))
                         (values `#(each+ ,x ,(reverse ys) ,z) ids)))))
                  ((x . y)
                   (let-values (((y ids) (cvt (syntax y) n ids)))
                     (let-values (((x ids) (cvt (syntax x) n ids)))
                       (values (cons x y) ids))))
                  (() (values '() ids))
                  (#(p ...)
                   (let-values (((p ids) (cvt (syntax (p ...)) n ids)))
                     (values `#(vector ,p) ids)))
                  (datum
                   (values `#(atom ,(sys.syntax->datum (syntax datum))) ids)))))
            (cvt pattern 0 '())))
      (syntax-case ctx ()
        ((_ expr (lits ...)) (for-all sys.identifier? (syntax (lits ...)))
         (syntax (stx-error expr "invalid syntax")))
        ((_ expr (lits ...) (pat fender body) cls* ...)
         (for-all sys.identifier? (syntax (lits ...)))
         (let-values (((pattern ids/levels)
                       (convert-pattern (syntax pat) (syntax (lits ...)))))
           (with-syntax ((pattern (sys.datum->syntax (syntax here) pattern))
                         (((ids . levels) ...) ids/levels))
             (syntax
               (let ((t expr))
                 (let ((ls/false (syntax-dispatch t 'pattern)))
                   (if (and ls/false (apply (lambda (ids ...) fender) ls/false))
                       (apply (lambda (ids ...) body) ls/false)
                       (syntax-match t (lits ...) cls* ...))))))))
        ((_ expr (lits ...) (pat body) cls* ...)
         (for-all sys.identifier? (syntax (lits ...)))
         (let-values (((pattern ids/levels)
                       (convert-pattern (syntax pat) (syntax (lits ...)))))
           (with-syntax ((pattern (sys.datum->syntax (syntax here) pattern))
                         (((ids . levels) ...) ids/levels))
             (syntax
               (let ((t expr))
                 (let ((ls/false (syntax-dispatch t 'pattern)))
                   (if ls/false
                       (apply (lambda (ids ...) body) ls/false)
                       (syntax-match t (lits ...) cls* ...))))))))
        ((_ expr (lits ...) (pat body) cls* ...)
         (syntax (syntax-match expr (lits ...) (pat #t body) cls* ...))))))

    
  (define parse-define
    (lambda (x)
      (syntax-match x ()
        ((_ (id . fmls) b b* ...) (id? id)
         (begin
           (verify-formals fmls x)
           (values id (cons 'defun x))))
        ((_ id val) (id? id)
         (values id (cons 'expr val)))
        ((_ id) (id? id)
         (values id (cons 'expr (bless '(void))))))))

  (define parse-define-syntax
    (lambda (x)
      (syntax-match x ()
        ((_ id val) (id? id) (values id val)))))

  ;;; scheme-stx takes a symbol and if it's in the 
  ;;; (psyntax system $all) library, it creates a fresh identifier
  ;;; that maps only the symbol to its label in that library.
  ;;; Symbols not in that library become fresh.
  (define scheme-stx-hashtable (make-eq-hashtable))
  (define scheme-stx
    (lambda (sym)
      (or (hashtable-ref scheme-stx-hashtable sym #f) 
          (let* ((subst
                  (library-subst
                    (find-library-by-name '(psyntax system $all))))
                 (stx (make-stx sym top-mark* '() '()))
                 (stx
                  (cond
                    ((assq sym subst) =>
                     (lambda (x)
                       (let ((name (car x)) (label (cdr x)))
                         (add-subst
                           (make-rib (list name) 
                             (list top-mark*) (list label) #f #f)
                           stx))))
                    (else stx))))
            (hashtable-set! scheme-stx-hashtable sym stx)
            stx))))

  ;;; macros
  (define lexical-var car)
  (define lexical-mutable? cdr)
  (define set-lexical-mutable! set-cdr!)
  (define add-lexical
    (lambda (lab lex r)
      (cons (cons* lab 'lexical lex #f) r)))
  ;;;
  (define add-lexicals
    (lambda (lab* lex* r)
      (cond
        ((null? lab*) r)
        (else
         (add-lexicals (cdr lab*) (cdr lex*)
           (add-lexical (car lab*) (car lex*) r))))))
  ;;;
  (define letrec-helper
    (lambda (e r mr build)
      (syntax-match e ()
        ((_ ((lhs* rhs*) ...) b b* ...)
         (if (not (valid-bound-ids? lhs*))
             (invalid-fmls-error e lhs*)
             (let ((lex* (map gen-lexical lhs*))
                   (lab* (map gen-label lhs*)))
               (let ((rib (make-full-rib lhs* lab*))
                     (r (add-lexicals lab* lex* r)))
                 (let ((body (chi-internal 
                               (add-subst rib (cons b b*)) r mr))
                       (rhs* (chi-expr* (map (lambda (x) (add-subst rib x))
                                             rhs*) r mr)))
                   (build no-source lex* rhs* body)))))))))

  (define letrec-transformer
    (lambda (e r mr) (letrec-helper e r mr build-letrec)))
  
  (define letrec*-transformer
    (lambda (e r mr) (letrec-helper e r mr build-letrec*)))

  (define fluid-let-syntax-transformer
    (lambda (e r mr)
      (define (lookup x)
        (let ([label 
        (or (id->label x)
                   (syntax-violation #f "unbound identifier" e x))])
          (let ([b (label->binding-no-fluids label r)])
            (cond
              [(and (pair? b) (eq? (car b) '$fluid)) (cdr b)]
              [else (syntax-violation #f "not a fluid identifier" e x)]))))
      (syntax-match e ()
        ((_ ((lhs* rhs*) ...) b b* ...)
         (if (not (valid-bound-ids? lhs*))
             (invalid-fmls-error e lhs*)
             (let ((lab* (map lookup lhs*))
                   (rhs* (map (lambda (x)
                                (make-eval-transformer
                                  (expand-transformer x mr)))
                               rhs*)))
               (chi-internal (cons b b*)
                 (append (map cons lab* rhs*) r)
                 (append (map cons lab* rhs*) mr))))))))

  (define type-descriptor-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ id) (id? id)
         (let* ((lab (id->label id))
                (b (label->binding lab r))
                (type (binding-type b)))
           (unless lab (raise-unbound-error id))
           (unless (and (eq? type '$rtd) (not (list? (binding-value b))))
             (stx-error e "not a record type"))
           (build-data no-source (binding-value b)))))))

  (define record-type-descriptor-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ id) (id? id)
         (let* ((lab (id->label id))
                (b (label->binding lab r))
                (type (binding-type b)))
           (unless lab (raise-unbound-error id))
           (unless (and (eq? type '$rtd) (list? (binding-value b)))
             (stx-error e "not a record type"))
           (chi-expr (car (binding-value b)) r mr))))))

  (define record-constructor-descriptor-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ id) (id? id)
         (let* ((lab (id->label id))
                (b (label->binding lab r))
                (type (binding-type b)))
           (unless lab (raise-unbound-error id))
           (unless (and (eq? type '$rtd) (list? (binding-value b)))
             (stx-error e "invalid type"))
           (chi-expr (cadr (binding-value b)) r mr))))))

  (define when-macro
    (lambda (e)
      (syntax-match e ()
        ((_ test e e* ...)
         (bless `(if ,test (begin ,e . ,e*)))))))

  (define unless-macro
    (lambda (e)
      (syntax-match e ()
        ((_ test e e* ...)
         (bless `(if (not ,test) (begin ,e . ,e*)))))))

  (define if-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ e0 e1 e2)
         (build-conditional no-source
           (chi-expr e0 r mr)
           (chi-expr e1 r mr)
           (chi-expr e2 r mr)))
        ((_ e0 e1)
         (build-conditional no-source
           (chi-expr e0 r mr)
           (chi-expr e1 r mr)
           (build-void))))))
 
  (define case-macro
    (lambda (e)
      (define (build-last cls)
        (syntax-match cls (else)
          ((else e e* ...) `(let () #f ,e . ,e*))
          (_ (build-one cls '(if #f #f)))))
      (define (build-one cls k)
        (syntax-match cls ()
          (((d* ...) e e* ...) 
           (if (= 1 (length d*))
             `(if (eqv? t ',(car d*)) (begin ,e . ,e*) ,k)
             `(if (memv t ',d*) (begin ,e . ,e*) ,k)))))
      (syntax-match e ()
        ((_ expr)
         (bless `(let ((t ,expr)) (if #f #f))))
        ((_ expr cls cls* ...)
         (bless 
           `(let ((t ,expr))
              ,(let f ((cls cls) (cls* cls*))
                 (if (null? cls*) 
                     (build-last cls)
                     (build-one cls (f (car cls*) (cdr cls*)))))))))))

  
  (define quote-transformer ; tricky to add source info here.
    (lambda (e r mr)
      (syntax-match e ()
        ((_ datum) (build-data no-source (stx->datum datum))))))
  
  (define case-lambda-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ (fmls* b* b** ...) ...)
         (let-values (((fmls* body*)
                       (chi-lambda-clause* e fmls*
                         (map cons b* b**) r mr)))
           (build-case-lambda (syntax-annotation e) fmls* body*))))))
           
  (define typed-case-lambda-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ (fmls* type-spec* b* b** ...) ...)
         (let-values (((fmls* body*)
                       (chi-lambda-clause* e fmls*
                         (map cons b* b**) r mr)))
           (build-typed-case-lambda (syntax-annotation e) fmls* (map stx->datum type-spec*) body*))))))
  
  (define lambda-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ fmls b b* ...)
         (let-values (((fmls body)
                       (chi-lambda-clause e fmls
                          (cons b b*) r mr)))
           (build-lambda (syntax-annotation e) fmls body))))))
           
  (define typed-lambda-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ fmls type-spec b b* ...)
         (let-values (((fmls body)
                       (chi-lambda-clause e fmls
                          (cons b b*) r mr)))
           (build-typed-lambda (syntax-annotation e) fmls (stx->datum type-spec) body))))))           
  
  (define bless
    (lambda (x)
      (mkstx
        (let f ((x x))
          (cond
            ((stx? x) x)
            ((pair? x) (cons (f (car x)) (f (cdr x))))
            ((symbol? x) (scheme-stx x))
            ((vector? x)
             (vector-map f x))
            (else x)))
        '() '() '())))
  
  (define with-syntax-macro
    (lambda (e)
      (syntax-match e ()
        ((_ ((pat* expr*) ...) b b* ...)
         (let ((idn*
                (let f ((pat* pat*))
                  (cond
                    ((null? pat*) '())
                    (else
                     (let-values (((pat idn*) (convert-pattern (car pat*) '())))
                       (append idn* (f (cdr pat*)))))))))
           (verify-formals (map car idn*) e)
           (let ((t* (generate-temporaries expr*)))
             (bless 
               `(let ,(map list t* expr*)
                  ,(let f ((pat* pat*) (t* t*))
                     (cond
                       ((null? pat*) `(let () ,b . ,b*))
                       (else
                        `(syntax-case ,(car t*) ()
                           (,(car pat*) ,(f (cdr pat*) (cdr t*)))
                           (_ (assertion-violation 'with-syntax
                                "pattern does not match value"
                                ',(car pat*)
                                ,(car t*)))))))))))))))

  (define (invalid-fmls-error stx fmls)
    (syntax-match fmls ()
      ((id* ... . last) 
       (let f ((id* (cond
                      ((id? last) (cons last id*))
                      ((syntax-null? last) id*)
                      (else 
                       (syntax-violation #f "not an identifier" stx last)))))
         (cond
           ((null? id*) (values))
           ((not (id? (car id*)))
            (syntax-violation #f "not an identifier" stx (car id*)))
           (else 
            (f (cdr id*))
            (when (bound-id-member? (car id*) (cdr id*))
              (syntax-violation #f "duplicate binding" stx (car id*)))))))
      (_ (syntax-violation #f "malformed binding form" stx fmls))))
  
  (define let-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ ((lhs* rhs*) ...) b b* ...)
         (if (valid-bound-ids? lhs*)
             (bless `((lambda ,lhs* ,b . ,b*) . ,rhs*))
             (invalid-fmls-error stx lhs*)))
        ((_ f ((lhs* rhs*) ...) b b* ...) (id? f)
         (if (valid-bound-ids? lhs*)
             (bless `((letrec ((,f (lambda ,lhs* ,b . ,b*))) ,f) . ,rhs*))
             (invalid-fmls-error stx lhs*))))))
  
  (define trace-let-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ f ((lhs* rhs*) ...) b b* ...) (id? f)
         (if (valid-bound-ids? lhs*)
             (bless 
               `((letrec ((,f (trace-lambda ,f ,lhs* ,b . ,b*))) ,f) . ,rhs*))
             (invalid-fmls-error stx lhs*))))))

  (define let-values-macro
    (lambda (stx)
      (define (rename x old* new*)
        (unless (id? x) 
          (syntax-violation #f "not an indentifier" stx x))
        (when (bound-id-member? x old*) 
          (syntax-violation #f "duplicate binding" stx x))
        (let ((y (gensym (syntax->datum x))))
          (values y (cons x old*) (cons y new*))))
      (define (rename* x* old* new*)
        (cond
          ((null? x*) (values '() old* new*))
          (else
           (let*-values (((x old* new*) (rename (car x*) old* new*))
                         ((x* old* new*) (rename* (cdr x*) old* new*)))
             (values (cons x x*) old* new*)))))
      (syntax-match stx ()
        ((_ () b b* ...)
         (cons* (bless 'let) '() b b*))
        ((_ ((lhs* rhs*) ...) b b* ...) 
         (bless 
           (let f ((lhs* lhs*) (rhs* rhs*) (old* '()) (new* '()))
             (cond
               ((null? lhs*) 
                `(let ,(map list old* new*) ,b . ,b*))
               (else
                (syntax-match (car lhs*) ()
                  ((x* ...) 
                   (let-values (((y* old* new*) (rename* x* old* new*)))
                     `(call-with-values 
                        (lambda () ,(car rhs*))
                        (lambda ,y* 
                          ,(f (cdr lhs*) (cdr rhs*) old* new*)))))
                  ((x* ... . x)
                   (let*-values (((y old* new*) (rename x old* new*))
                                 ((y* old* new*) (rename* x* old* new*)))
                     `(call-with-values 
                        (lambda () ,(car rhs*))
                        (lambda ,(append y* y)
                          ,(f (cdr lhs*) (cdr rhs*)
                              old* new*)))))
                  (others
                   (syntax-violation #f "malformed bindings"
                      stx others)))))))))))

  (define let*-values-macro
    (lambda (stx)
      (define (check x*) 
        (unless (null? x*)
          (let ((x (car x*)))
            (unless (id? x)
              (syntax-violation #f "not an identifier" stx x))
            (check (cdr x*))
            (when (bound-id-member? x (cdr x*))
              (syntax-violation #f "duplicate identifier" stx x)))))
      (syntax-match stx ()
        ((_ () b b* ...)
         (cons* (bless 'let) '() b b*))
        ((_ ((lhs* rhs*) ...) b b* ...)
         (bless 
           (let f ((lhs* lhs*) (rhs* rhs*))
             (cond
               ((null? lhs*) 
                `(begin ,b . ,b*))
               (else
                (syntax-match (car lhs*) ()
                  ((x* ...) 
                   (begin
                     (check x*)
                     `(call-with-values 
                        (lambda () ,(car rhs*))
                        (lambda ,x*
                          ,(f (cdr lhs*) (cdr rhs*))))))
                  ((x* ... . x)
                   (begin
                     (check (cons x x*))
                     `(call-with-values 
                        (lambda () ,(car rhs*))
                        (lambda ,(append x* x)
                          ,(f (cdr lhs*) (cdr rhs*))))))
                  (others
                   (syntax-violation #f "malformed bindings"
                      stx others)))))))))))
  
  (define trace-lambda-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ who (fmls ...) b b* ...)
         (if (valid-bound-ids? fmls)
             (bless `(make-traced-procedure ',who
                       (lambda ,fmls ,b . ,b*)))
             (invalid-fmls-error stx fmls)))
        ((_  who (fmls ... . last) b b* ...)
         (if (valid-bound-ids? (cons last fmls))
             (bless `(make-traced-procedure ',who
                       (lambda (,@fmls . ,last) ,b . ,b*)))
             (invalid-fmls-error stx (append fmls last)))))))
  
  (define trace-define-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ (who fmls ...) b b* ...)
         (if (valid-bound-ids? fmls)
             (bless `(define ,who
                       (make-traced-procedure ',who
                         (lambda ,fmls ,b . ,b*))))
             (invalid-fmls-error stx fmls)))
        ((_ (who fmls ... . last) b b* ...)
         (if (valid-bound-ids? (cons last fmls))
             (bless `(define ,who
                       (make-traced-procedure ',who
                         (lambda (,@fmls . ,last) ,b . ,b*))))
             (invalid-fmls-error stx (append fmls last))))
        ((_ who expr)
         (if (id? who)
             (bless `(define ,who
                       (let ((v ,expr))
                         (if (procedure? v)
                             (make-traced-procedure ',who v)
                             v))))
             (stx-error stx "invalid name"))))))
  
  (define trace-define-syntax-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ who expr)
         (if (id? who)
             (bless 
               `(define-syntax ,who
                  (make-traced-macro ',who ,expr)))
             (stx-error stx "invalid name"))))))

  (define trace-let/rec-syntax
    (lambda (who)
      (lambda (stx)
        (syntax-match stx ()
          ((_ ((lhs* rhs*) ...) b b* ...)
           (if (valid-bound-ids? lhs*)
               (let ((rhs* (map (lambda (lhs rhs) 
                                  `(make-traced-macro ',lhs ,rhs))
                                lhs* rhs*)))
                 (bless `(,who ,(map list lhs* rhs*) ,b . ,b*)))
               (invalid-fmls-error stx lhs*)))))))

  (define trace-let-syntax-macro
    (trace-let/rec-syntax 'let-syntax))

  (define trace-letrec-syntax-macro
    (trace-let/rec-syntax 'letrec-syntax))
  
  (define guard-macro
    (lambda (x)
      (define (gen-clauses con outerk clause*) 
        (define (f x k) 
          (syntax-match x (=>) 
            ((e => p) 
             (let ((t (gensym)))
               `(let ((,t ,e)) 
                  (if ,t (,p ,t) ,k))))
            ((e) 
             (let ((t (gensym)))
               `(let ((,t ,e))
                  (if ,t ,t ,k))))
            ((e v v* ...) 
             `(if ,e (begin ,v ,@v*) ,k))
            (_ (stx-error x "invalid guard clause"))))
        (define (f* x*)
          (syntax-match x* (else)
            (()
             (let ((g (gensym)))
               (values `((lambda () (raise-continuable ,con))) g)))
            (((else e e* ...))
             (values `(begin ,e ,@e*) #f))
            ((cls . cls*) 
             (let-values (((e g) (f* cls*)))
               (values (f cls e) g)))
            (others (stx-error others "invalid guard clause"))))
        (let-values (((code raisek) (f* clause*)))
          (if raisek
            `((call/cc
                (lambda (,raisek)
                  (,outerk 
                    (lambda () ,code)))))
            `(,outerk (lambda () ,code)))))
      (syntax-match x ()
        ((_ (con clause* ...) b b* ...)
         (id? con)
         (let ((outerk (gensym)))
           (bless
             `((call/cc
                 (lambda (,outerk)
                     (with-exception-handler
                       (lambda (,con)
                         ,(gen-clauses con outerk clause*))
                       (lambda () 
                         (call-with-values 
                          (lambda () ,b ,@b*)
                          (lambda args
                              (,outerk (lambda ()
                                         (apply values args))))))))))))))))

  (define define-enumeration-macro
    (lambda (stx) 
      (define (set? x)
        (or (null? x) 
            (and (not (memq (car x) (cdr x)))
                 (set? (cdr x)))))
      (define (remove-dups ls)
        (cond
          ((null? ls) '())
          (else 
           (cons (car ls) 
              (remove-dups (remq (car ls) (cdr ls)))))))
      (syntax-match stx ()
        ((_ name (id* ...) maker) 
         (and (id? name) (id? maker) (for-all id? id*))
         (let ((name* (remove-dups (syntax->datum id*))) (mk (gensym))) 
           (bless 
             `(begin
                ;;; can be constructed at compile time
                ;;; but ....  it's not worth it.
                ;;; also, generativity of defined enum types
                ;;; is completely unspecified, making them just
                ;;; more useless than they really are.
                ;;; eventually, I'll make them all compile-time
                ;;; generative just to piss some known people off.
                (define ,mk 
                  (enum-set-constructor 
                    (make-enumeration ',name*)))
                (define-syntax ,name 
                  (lambda (x) 
                    (syntax-case x () 
                      ((_ n)
                       (identifier? (syntax n))
                       (if (memq (syntax->datum (syntax n)) ',name*) 
                           (syntax 'n)
                           (syntax-violation ',name
                              "not a member of set"
                              x (syntax n)))))))
                (define-syntax ,maker
                  (lambda (x)
                    (syntax-case x ()
                      ((_ n* ...)
                       (begin
                         (for-each
                           (lambda (n) 
                              (unless (identifier? n) 
                                (syntax-violation
                                  ',maker 
                                  "non-identifier argument"
                                  x
                                  n))
                              (unless (memq (syntax->datum n) ',name*)
                                (syntax-violation
                                  ',maker
                                  "not a member of set"
                                  x
                                  n)))
                           (syntax (n* ...)))
                         (syntax (,mk '(n* ...)))))))))))))))

  (define time-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ expr)
         (let ((str 
                (let-values (((p e) (open-string-output-port)))
                  (write (syntax->datum expr) p)
                  (e))))
           (bless `(time-it ,str (lambda () ,expr))))))))
  
  (define delay-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ expr)
         (bless `(make-promise (lambda () ,expr)))))))
  
  (define assert-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ expr)
         (let ((pos (or (expression-position stx)
                        (expression-position expr))))
           (bless 
             `(unless ,expr (assertion-error ',expr ',pos))))))))
  
  (define endianness-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ e)
         (case (syntax->datum e)
           ((little) (bless `'little))
           ((big)    (bless `'big))
           (else (stx-error stx "endianness must be big or little")))))))
  
  (define identifier-syntax-macro
    (lambda (stx)
      (syntax-match stx (set!)
        ((_ expr)
         (bless `(lambda (x)
                   (syntax-case x ()
                     (id (identifier? (syntax id)) (syntax ,expr))
                     ((id e* ...) (identifier? (syntax id))
                      (cons (syntax ,expr) (syntax (e* ...))))))))
        ((_ (id1 expr1) ((set! id2 expr2) expr3))
         (and (id? id1) (id? id2) (id? expr2))
         (bless `(cons 'macro!
                   (lambda (x)
                     (syntax-case x (set!)
                       (id (identifier? (syntax id)) (syntax ,expr1))
                       ((set! id ,expr2) (syntax ,expr3))
                       ((id e* ...) (identifier? (syntax id)) (syntax (,expr1 e* ...)))))))))))
  
  (define do-macro
    (lambda (stx)
      (define bind
        (lambda (x)
          (syntax-match x ()
            ((x init)      `(,x ,init ,x))
            ((x init step) `(,x ,init ,step))
            (_  (stx-error stx "invalid binding")))))
      (syntax-match stx ()
        ((_ (binding* ...)
            (test expr* ...)
            command* ...)
         (syntax-match (map bind binding*) ()
           (((x* init* step*) ...)
            (if (valid-bound-ids? x*)
                (bless
                  `(letrec ((loop
                             (lambda ,x*
                               (if ,test
                                 (begin (if #f #f) ,@expr*)
                                 (begin
                                   ,@command*
                                   (loop ,@step*))))))
                     (loop ,@init*)))
                (stx-error stx "invalid bindings"))))))))
  
  (define let*-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ ((lhs* rhs*) ...) b b* ...) (for-all id? lhs*)
         (bless
           (let f ((x* (map list lhs* rhs*)))
             (cond
               ((null? x*) `(let () ,b . ,b*))
               ((null? (cdr x*)) `(let (,(car x*)) ,b . ,b*))
               (else `(let (,(car x*)) ,(f (cdr x*)))))))))))
  
  (define or-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_) #f)
        ((_ e e* ...)
         (bless
           (let f ((e e) (e* e*))
             (cond
               ((null? e*) `(begin #f ,e))
               (else
                `(let ((t ,e))
                   (if t t ,(f (car e*) (cdr e*))))))))))))
  
  (define and-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_) #t)
        ((_ e e* ...)
         (bless
           (let f ((e e) (e* e*))
             (cond
               ((null? e*) `(begin #f ,e))
               (else `(if ,e ,(f (car e*) (cdr e*)) #f)))))))))
  
  (define cond-macro
    (lambda (stx)
      (syntax-match stx ()
        ((_ cls cls* ...)
         (bless
           (let f ((cls cls) (cls* cls*))
             (cond
               ((null? cls*)
                (syntax-match cls (else =>)
                  ((else e e* ...) `(let () #f ,e . ,e*))
                  ((e => p) `(let ((t ,e)) (if t (,p t))))
                  ((e) `(or ,e (if #f #f)))
                  ((e e* ...) `(if ,e (begin . ,e*)))
                  (_ (stx-error stx "invalid last clause"))))
               (else
                (syntax-match cls (else =>)
                  ((else e e* ...) (stx-error stx "incorrect position of keyword else"))
                  ((e => p) `(let ((t ,e)) (if t (,p t) ,(f (car cls*) (cdr cls*)))))
                  ((e) `(or ,e ,(f (car cls*) (cdr cls*))))
                  ((e e* ...) `(if ,e (begin . ,e*) ,(f (car cls*) (cdr cls*))))
                  (_ (stx-error stx "invalid last clause")))))))))))
  
  (begin ; module (include-macro include-into-macro)
         ; no module to keep portable! 
         ; dump everything in top-level, sure.
    (define (do-include stx id filename)
      (let ((filename (stx->datum filename)))
        (unless (and (string? filename) (id? id))
          (stx-error stx))
        (cons 
          (bless 'begin)
          (call-with-input-file filename
            (lambda (p)
              (let f ((ls '()))
                (let ((x (read-annotated p)))
                  (cond
                    ((eof-object? x) (reverse ls))
                    (else
                     (f (cons (datum->stx id x) ls)))))))))))
    (define include-macro
      (lambda (e)
        (syntax-match e ()
          ((id filename)
           (do-include e id filename)))))
    (define include-into-macro
      (lambda (e)
        (syntax-match e ()
          ((_ id filename)
           (do-include e id filename))))))

  
  (define syntax-rules-macro
    (lambda (e)
      (syntax-match e ()
        ((_ (lits ...)
            (pat* tmp*) ...)
         (begin
           (verify-literals lits e)
           (bless `(lambda (x)
                     (syntax-case x ,lits
                       ,@(map (lambda (pat tmp)
                                (syntax-match pat ()
                                  ((_ . rest) 
                                   `((g . ,rest) (syntax ,tmp)))
                                  (_ 
                                   (syntax-violation #f
                                      "invalid syntax-rules pattern" 
                                      e pat))))
                              pat* tmp*)))))))))
  
  (define quasiquote-macro
    (let ()
      (define (datum x)
        (list (scheme-stx 'quote) (mkstx x top-mark* '() '())))
      (define-syntax app
        (syntax-rules (quote)
          ((_ 'x arg* ...)
           (list (scheme-stx 'x) arg* ...))))
      (define-syntax app*
        (syntax-rules (quote)
          ((_ 'x arg* ... last)
           (cons* (scheme-stx 'x) arg* ... last))))
      (define quasicons*
        (lambda (x y)
          (let f ((x x))
            (if (null? x) y (quasicons (car x) (f (cdr x)))))))
      (define quasicons
        (lambda (x y)
          (syntax-match y (quote list)
            ((quote dy)
             (syntax-match x (quote)
               ((quote dx) (app 'quote (cons dx dy)))
               (_
                (syntax-match dy ()
                  (() (app 'list x))
                  (_  (app 'cons x y))))))
            ((list stuff ...)
             (app* 'list x stuff))
            (_ (app 'cons x y)))))
      (define quasiappend
        (lambda (x y)
          (let ((ls (let f ((x x))
                      (if (null? x)
                          (syntax-match y (quote)
                            ((quote ()) '())
                            (_ (list y)))
                          (syntax-match (car x) (quote)
                            ((quote ()) (f (cdr x)))
                            (_ (cons (car x) (f (cdr x)))))))))
            (cond
              ((null? ls) (app 'quote '()))
              ((null? (cdr ls)) (car ls))
              (else (app* 'append ls))))))
      (define quasivector
        (lambda (x)
          (let ((pat-x x))
            (syntax-match pat-x (quote)
              ((quote (x* ...)) (app 'quote (list->vector x*)))
              (_ (let f ((x x) (k (lambda (ls) (app* 'vector ls))))
                   (syntax-match x (quote list cons)
                     ((quote (x* ...))
                      (k (map (lambda (x) (app 'quote x)) x*)))
                     ((list x* ...)
                      (k x*))
                     ((cons x y)
                      (f y (lambda (ls) (k (cons x ls)))))
                     (_ (app 'list->vector pat-x)))))))))
      (define vquasi
        (lambda (p lev)
          (syntax-match p ()
            ((p . q)
             (syntax-match p (unquote unquote-splicing)
               ((unquote p ...)
                (if (= lev 0)
                    (quasicons* p (vquasi q lev))
                    (quasicons
                      (quasicons (datum 'unquote)
                                 (quasi p (- lev 1)))
                      (vquasi q lev))))
               ((unquote-splicing p ...)
                (if (= lev 0)
                    (quasiappend p (vquasi q lev))
                    (quasicons
                      (quasicons
                        (datum 'unquote-splicing)
                        (quasi p (- lev 1)))
                      (vquasi q lev))))
               (p (quasicons (quasi p lev) (vquasi q lev)))))
            (() (app 'quote '())))))
      (define quasi
        (lambda (p lev)
          (syntax-match p (unquote unquote-splicing quasiquote)
            ((unquote p)
             (if (= lev 0)
                 p
                 (quasicons (datum 'unquote) (quasi (list p) (- lev 1)))))
            (((unquote p ...) . q)
             (if (= lev 0)
                 (quasicons* p (quasi q lev))
                 (quasicons
                   (quasicons (datum 'unquote)
                              (quasi p (- lev 1)))
                   (quasi q lev))))
            (((unquote-splicing p ...) . q)
             (if (= lev 0)
                 (quasiappend p (quasi q lev))
                 (quasicons
                   (quasicons (datum 'unquote-splicing)
                     (quasi p (- lev 1)))
                   (quasi q lev))))
            ((quasiquote p)
             (quasicons (datum 'quasiquote)
                        (quasi (list p) (+ lev 1))))
            ((p . q) (quasicons (quasi p lev) (quasi q lev)))
            (#(x ...) (not (stx? x)) (quasivector (vquasi x lev)))
            (p (app 'quote p)))))
      (lambda (x)
        (syntax-match x ()
          ((_ e) (quasi e 0))))))
  
  (define quasisyntax-macro
    (let () ;;; FIXME: not really correct
      (define quasi
        (lambda (p lev)
          (syntax-match p (unsyntax unsyntax-splicing quasisyntax)
            ((unsyntax p)
             (if (= lev 0)
                 (let ((g (gensym)))
                   (values (list g) (list p) g))
                 (let-values (((lhs* rhs* p) (quasi p (- lev 1))))
                   (values lhs* rhs* (list 'unsyntax p)))))
            (unsyntax (= lev 0)
             (stx-error p "incorrect use of unsyntax"))
            (((unsyntax p* ...) . q)
             (let-values (((lhs* rhs* q) (quasi q lev)))
               (if (= lev 0)
                   (let ((g* (map (lambda (x) (gensym)) p*)))
                     (values 
                       (append g* lhs*)
                       (append p* rhs*)
                       (append g* q)))
                   (let-values (((lhs2* rhs2* p*) (quasi p* (- lev 1))))
                     (values 
                       (append lhs2* lhs*)
                       (append rhs2* rhs*)
                       `((unsyntax . ,p*) . ,q))))))
            (((unsyntax-splicing p* ...) . q)
             (let-values (((lhs* rhs* q) (quasi q lev)))
               (if (= lev 0)
                   (let ((g* (map (lambda (x) (gensym)) p*)))
                     (values 
                       (append 
                         (map (lambda (g) `(,g ...)) g*)
                         lhs*)
                       (append p* rhs*)
                       (append
                         (apply append 
                           (map (lambda (g) `(,g ...)) g*))
                         q)))
                   (let-values (((lhs2* rhs2* p*) (quasi p* (- lev 1))))
                     (values 
                       (append lhs2* lhs*)
                       (append rhs2* rhs*)
                       `((unsyntax-splicing . ,p*) . ,q))))))
            (unsyntax-splicing (= lev 0)
             (stx-error p "incorrect use of unsyntax-splicing"))
            ((quasisyntax p)
             (let-values (((lhs* rhs* p) (quasi p (+ lev 1))))
                (values lhs* rhs* `(quasisyntax ,p))))
            ((p . q)
             (let-values (((lhs* rhs* p) (quasi p lev))
                          ((lhs2* rhs2* q) (quasi q lev)))
                (values (append lhs2* lhs*)
                        (append rhs2* rhs*)
                        (cons p q))))
            (#(x* ...) 
             (let-values (((lhs* rhs* x*) (quasi x* lev)))
               (values lhs* rhs* (list->vector x*))))
            (_ (values '() '() p)))))
      (lambda (x)
        (syntax-match x ()
          ((_ e)
           (let-values (((lhs* rhs* v) (quasi e 0)))
              (bless
                `(syntax-case (list ,@rhs*) ()
                   (,lhs* (syntax ,v))))))))))


  (define define-struct-macro
    (lambda (stx)
      (stx-error stx "define-struct not supported")))

  (define define-record-type-macro
    (lambda (x)
      (define (id ctxt . str*)
        (datum->syntax ctxt 
          (string->symbol 
            (apply string-append 
              (map (lambda (x) 
                     (cond
                       ((symbol? x) (symbol->string x))
                       ((string? x) x)
                       (else (assertion-violation 'define-record-type "BUG"))))
                   str*)))))
      (define (get-record-name spec)
        (syntax-match spec ()
          ((foo make-foo foo?) foo)
          (foo foo)))
      (define (get-record-constructor-name spec)
        (syntax-match spec ()
          ((foo make-foo foo?) make-foo)
          (foo (id? foo) (id foo "make-" (stx->datum foo)))))
      (define (get-record-predicate-name spec)
        (syntax-match spec ()
          ((foo make-foo foo?) foo?)
          (foo (id? foo) (id foo (stx->datum foo) "?"))))
      (define (get-clause id ls)
        (syntax-match ls ()
          (() #f)
          (((x . rest) . ls)
           (if (free-id=? (bless id) x)
               `(,x . ,rest)
               (get-clause id ls)))))
      (define (foo-rtd-code name clause* parent-rtd-code) 
        (define (convert-field-spec* ls)
          (list->vector
            (map (lambda (x) 
                   (syntax-match x (mutable immutable)
                     ((mutable name . rest) `(mutable ,name))
                     ((immutable name . rest) `(immutable ,name))
                     (name `(immutable ,name))))
               ls)))
        (let ((uid-code
               (syntax-match (get-clause 'nongenerative clause*) ()
                 ((_)     `',(gensym))
                 ((_ uid) `',uid)
                 (_       #f)))
              (sealed?
               (syntax-match (get-clause 'sealed clause*) ()
                 ((_ #t) #t)
                 (_      #f)))
              (opaque?
               (syntax-match (get-clause 'opaque clause*) ()
                 ((_ #t) #t)
                 (_      #f)))
              (fields 
               (syntax-match (get-clause 'fields clause*) ()
                 ((_ field-spec* ...)
                  `(quote ,(convert-field-spec* field-spec*)))
                 (_ ''#()))))
          (bless
            `(make-record-type-descriptor ',name
               ,parent-rtd-code 
               ,uid-code ,sealed? ,opaque? ,fields))))
      (define (parent-rtd-code clause*)
        (syntax-match (get-clause 'parent clause*) ()
          ((_ name) `(record-type-descriptor ,name))
          (#f (syntax-match (get-clause 'parent-rtd clause*) ()
                ((_ rtd rcd) rtd)
                (#f #f)))))
      (define (parent-rcd-code clause*)
        (syntax-match (get-clause 'parent clause*) ()
          ((_ name) `(record-constructor-descriptor ,name))
          (#f (syntax-match (get-clause 'parent-rtd clause*) ()
                ((_ rtd rcd) rcd)
                (#f #f)))))
      (define (foo-rcd-code clause* foo-rtd protocol parent-rcd-code)
        `(make-record-constructor-descriptor ,foo-rtd
           ,parent-rcd-code ,protocol))
      (define (get-protocol-code clause*)
        (syntax-match (get-clause 'protocol clause*) ()
          ((_ expr) expr)
          (_        #f)))
      (define (get-fields clause*)
        (syntax-match clause* (fields)
          (() '())
          (((fields f* ...) . _) f*)
          ((_ . rest) (get-fields rest))))
      (define (get-mutator-indices fields)
        (let f ((fields fields) (i 0))
          (syntax-match fields (mutable)
            (() '())
            (((mutable . _) . rest)
             (cons i (f rest (+ i 1))))
            ((_ . rest)
             (f rest (+ i 1))))))
      (define (get-mutators foo fields)
        (define (gen-name x) 
          (datum->syntax foo
            (string->symbol 
              (string-append
                (symbol->string (syntax->datum foo))
                "-"
                (symbol->string (syntax->datum x))
                "-set!"))))
        (let f ((fields fields))
          (syntax-match fields (mutable)
            (() '())
            (((mutable name accessor mutator) . rest) 
             (cons mutator (f rest)))
            (((mutable name) . rest)
             (cons (gen-name name) (f rest)))
            ((_ . rest) (f rest)))))
      (define (get-accessors foo fields)
        (define (gen-name x) 
          (datum->syntax foo
            (string->symbol 
              (string-append
                (symbol->string (syntax->datum foo))
                "-"
                (symbol->string (syntax->datum x))))))
        (map 
          (lambda (field)
            (syntax-match field (mutable immutable)
              ((mutable name accessor mutator) (id? accessor) accessor)
              ((immutable name accessor)       (id? accessor) accessor)
              ((mutable name)                  (id? name) (gen-name name))
              ((immutable name)                (id? name) (gen-name name))
              (name                            (id? name) (gen-name name))
              (others (stx-error field "invalid field spec"))))
          fields))
      (define (enumerate ls)
        (let f ((ls ls) (i 0))
          (cond
            ((null? ls) '())
            (else (cons i (f (cdr ls) (+ i 1)))))))
      (define (do-define-record namespec clause*)
        (let* ((foo (get-record-name namespec))
               (foo-rtd (gensym))
               (foo-rcd (gensym))
               (protocol (gensym))
               (make-foo (get-record-constructor-name namespec))
               (fields (get-fields clause*))
               (idx* (enumerate fields))
               (foo-x* (get-accessors foo fields))
               (set-foo-x!* (get-mutators foo fields))
               (set-foo-idx* (get-mutator-indices fields))
               (foo? (get-record-predicate-name namespec))
               (foo-rtd-code (foo-rtd-code foo clause* (parent-rtd-code clause*)))
               (foo-rcd-code (foo-rcd-code clause* foo-rtd protocol (parent-rcd-code clause*)))
               (protocol-code (get-protocol-code clause*)))
          (bless
            `(begin
               (define ,foo-rtd ,foo-rtd-code)
               (define ,protocol ,protocol-code)
               (define ,foo-rcd ,foo-rcd-code)
               (define-syntax ,foo 
                 (list '$rtd (syntax ,foo-rtd) (syntax ,foo-rcd)))
               (define ,foo? (record-predicate ,foo-rtd))
               (define ,make-foo (record-constructor ,foo-rcd))
               ,@(map 
                   (lambda (foo-x idx)
                     `(define ,foo-x (record-accessor ,foo-rtd ,idx)))
                   foo-x* idx*)
               ,@(map 
                   (lambda (set-foo-x! idx)
                     `(define ,set-foo-x! (record-mutator ,foo-rtd ,idx)))
                   set-foo-x!* set-foo-idx*)))))
      (define (verify-clauses x cls*)
        (define valid-kwds 
          (map bless 
            '(fields parent parent-rtd protocol sealed opaque nongenerative)))
        (define (free-id-member? x ls) 
          (and (pair? ls) 
               (or (free-id=? x (car ls)) 
                   (free-id-member? x (cdr ls)))))
        (let f ((cls* cls*) (seen* '()))
          (unless (null? cls*)
            (syntax-match (car cls*) ()
              ((kwd . rest)
               (cond
                 ((or (not (id? kwd)) 
                      (not (free-id-member? kwd valid-kwds)))
                  (stx-error kwd "not a valid define-record-type keyword"))
                 ((bound-id-member? kwd seen*)
                  (syntax-violation #f  
                     "duplicate use of keyword "
                     x kwd))
                 (else (f (cdr cls*) (cons kwd seen*)))))
              (cls
               (stx-error cls "malformed define-record-type clause"))))))
      (syntax-match x ()
        ((_ namespec clause* ...)
         (begin
           (verify-clauses x clause*)
           (do-define-record namespec clause*))))))
  
  (define define-condition-type-macro
    (lambda (x)
      (define (mkname name suffix)
        (datum->syntax name 
           (string->symbol 
             (string-append 
               (symbol->string (syntax->datum name))
               suffix))))
      (syntax-match x ()
        ((ctxt name super constructor predicate (field* accessor*) ...)
         (and (id? name) 
              (id? super)
              (id? constructor)
              (id? predicate)
              (for-all id? field*)
              (for-all id? accessor*))
         (let ((aux-accessor* (map (lambda (x) (gensym)) accessor*)))
           (bless
              `(begin
                 (define-record-type (,name ,constructor ,(gensym))
                    (parent ,super)
                    (fields ,@(map (lambda (field aux) 
                                     `(immutable ,field ,aux))
                                   field* aux-accessor*))
                    (nongenerative)
                    (sealed #f) 
                    (opaque #f))
                 (define ,predicate (condition-predicate
                                      (record-type-descriptor ,name)))
                 ,@(map 
                     (lambda (accessor aux)
                        `(define ,accessor 
                           (condition-accessor
                             (record-type-descriptor ,name) ,aux)))
                     accessor* aux-accessor*))))))))
  
  (define incorrect-usage-macro
    (lambda (e) (stx-error e "incorrect usage of auxiliary keyword")))
  
  (define parameterize-macro
    (lambda (e)
      (syntax-match e ()
        ((_ () b b* ...) 
         (bless `(let () ,b . ,b*)))
        ((_ ((olhs* orhs*) ...) b b* ...)
         (let ((lhs* (generate-temporaries olhs*))
               (rhs* (generate-temporaries orhs*)))
           (bless
             `((lambda ,(append lhs* rhs*)
                 (let ((swap (lambda () 
                               ,@(map (lambda (lhs rhs)
                                        `(let ((t (,lhs)))
                                           (,lhs ,rhs)
                                           (set! ,rhs t)))
                                      lhs* rhs*))))
                   (dynamic-wind 
                     swap
                     (lambda () ,b . ,b*)
                     swap)))
               ,@(append olhs* orhs*))))))))

  
  (define foreign-call-transformer
    (lambda (e r mr)
      (syntax-match e ()
        ((_ name arg* ...)
         (build-foreign-call no-source
           (chi-expr name r mr)
           (chi-expr* arg* r mr))))))

  ;; p in pattern:                        matches:
  ;;   ()                                 empty list
  ;;   _                                  anything (no binding created)
  ;;   any                                anything
  ;;   (p1 . p2)                          pair
  ;;   #(free-id <key>)                   <key> with free-identifier=?
  ;;   each-any                           any proper list
  ;;   #(each p)                          (p*)
  ;;   #(each+ p1 (p2_1 ... p2_n) p3)      (p1* (p2_n ... p2_1) . p3)
  ;;   #(vector p)                        #(x ...) if p matches (x ...)
  ;;   #(atom <object>)                   <object> with "equal?"
  (define convert-pattern
   ; returns syntax-dispatch pattern & ids
    (lambda (pattern keys)
      (define cvt*
        (lambda (p* n ids)
          (if (null? p*)
              (values '() ids)
              (let-values (((y ids) (cvt* (cdr p*) n ids)))
                (let-values (((x ids) (cvt (car p*) n ids)))
                  (values (cons x y) ids))))))
      (define cvt
        (lambda (p n ids)
          (syntax-match p ()
            (id (id? id)
             (cond
               ((bound-id-member? p keys)
                (values `#(free-id ,p) ids))
               ((free-id=? p (scheme-stx '_))
                (values '_ ids))
               (else (values 'any (cons (cons p n) ids)))))
            ((p dots) (ellipsis? dots)
             (let-values (((p ids) (cvt p (+ n 1) ids)))
               (values
                 (if (eq? p 'any) 'each-any `#(each ,p))
                 ids)))
            ((x dots ys ... . z) (ellipsis? dots)
             (let-values (((z ids) (cvt z n ids)))
               (let-values (((ys ids) (cvt* ys n ids)))
                 (let-values (((x ids) (cvt x (+ n 1) ids)))
                   (values `#(each+ ,x ,(reverse ys) ,z) ids)))))
            ((x . y)
             (let-values (((y ids) (cvt y n ids)))
               (let-values (((x ids) (cvt x n ids)))
                 (values (cons x y) ids))))
            (() (values '() ids))
            (#(p ...) (not (stx? p))
             (let-values (((p ids) (cvt p n ids)))
               (values `#(vector ,p) ids)))
            (datum
             (values `#(atom ,(stx->datum datum)) ids)))))
      (cvt pattern 0 '())))

  (define syntax-dispatch
    (lambda (e p)
      (define stx^
        (lambda (e m* s* ae*)
          (if (and (null? m*) (null? s*) (null? ae*))
              e
              (mkstx e m* s* ae*))))
      (define match-each
        (lambda (e p m* s* ae*)
          (cond
            ((pair? e)
             (let ((first (match (car e) p m* s* ae* '())))
               (and first
                    (let ((rest (match-each (cdr e) p m* s* ae*)))
                      (and rest (cons first rest))))))
            ((null? e) '())
            ((stx? e)
             (and (not (top-marked? m*))
               (let-values (((m* s* ae*) (join-wraps m* s* ae* e)))
                 (match-each (stx-expr e) p m* s* ae*))))
            ((annotation? e)
             (match-each (annotation-expression e) p m* s* ae*))
            (else #f))))
      (define match-each+
        (lambda (e x-pat y-pat z-pat m* s* ae* r)
          (let f ((e e) (m* m*) (s* s*) (ae* ae*))
            (cond
              ((pair? e)
               (let-values (((xr* y-pat r) (f (cdr e) m* s* ae*)))
                 (if r
                     (if (null? y-pat)
                         (let ((xr (match (car e) x-pat m* s* ae* '())))
                           (if xr
                               (values (cons xr xr*) y-pat r)
                               (values #f #f #f)))
                         (values
                           '()
                           (cdr y-pat)
                           (match (car e) (car y-pat) m* s* ae* r)))
                     (values #f #f #f))))
              ((stx? e)
               (if (top-marked? m*)
                   (values '() y-pat (match e z-pat m* s* ae* r))
                   (let-values (((m* s* ae*) (join-wraps m* s* ae* e)))
                     (f (stx-expr e) m* s* ae*))))
              ((annotation? e) 
               (f (annotation-expression e) m* s* ae*))
              (else (values '() y-pat (match e z-pat m* s* ae* r)))))))
      (define match-each-any
        (lambda (e m* s* ae*)
          (cond
            ((pair? e)
             (let ((l (match-each-any (cdr e) m* s* ae*)))
               (and l (cons (stx^ (car e) m* s* ae*) l))))
            ((null? e) '())
            ((stx? e)
             (and (not (top-marked? m*))
               (let-values (((m* s* ae*) (join-wraps m* s* ae* e)))
                 (match-each-any (stx-expr e) m* s* ae*))))
            ((annotation? e) 
             (match-each-any (annotation-expression e) m* s* ae*))
            (else #f))))
      (define match-empty
        (lambda (p r)
          (cond
            ((null? p) r)
            ((eq? p '_) r)
            ((eq? p 'any) (cons '() r))
            ((pair? p) (match-empty (car p) (match-empty (cdr p) r)))
            ((eq? p 'each-any) (cons '() r))
            (else
             (case (vector-ref p 0)
               ((each) (match-empty (vector-ref p 1) r))
               ((each+)
                (match-empty
                  (vector-ref p 1)
                  (match-empty
                    (reverse (vector-ref p 2))
                    (match-empty (vector-ref p 3) r))))
               ((free-id atom) r)
               ((scheme-id atom) r)
               ((vector) (match-empty (vector-ref p 1) r))
               (else (assertion-violation 'syntax-dispatch "invalid pattern" p)))))))
      (define combine
        (lambda (r* r)
          (if (null? (car r*))
              r
              (cons (map car r*) (combine (map cdr r*) r)))))
      (define match*
        (lambda (e p m* s* ae* r)
          (cond
            ((null? p) (and (null? e) r))
            ((pair? p)
             (and (pair? e)
                  (match (car e) (car p) m* s* ae*
                    (match (cdr e) (cdr p) m* s* ae* r))))
            ((eq? p 'each-any)
             (let ((l (match-each-any e m* s* ae*))) (and l (cons l r))))
            (else
             (case (vector-ref p 0)
               ((each)
                (if (null? e)
                    (match-empty (vector-ref p 1) r)
                    (let ((r* (match-each e (vector-ref p 1) m* s* ae*)))
                      (and r* (combine r* r)))))
               ((free-id)
                (and (symbol? e)
                     (top-marked? m*)
                     (free-id=? (stx^ e m* s* ae*) (vector-ref p 1))
                     r))
               ((scheme-id)
                (and (symbol? e)
                     (top-marked? m*)
                     (free-id=? (stx^ e m* s* ae*) 
                                (scheme-stx (vector-ref p 1)))
                     r))
               ((each+)
                (let-values (((xr* y-pat r)
                              (match-each+ e (vector-ref p 1)
                                (vector-ref p 2) (vector-ref p 3) m* s* ae* r)))
                  (and r
                       (null? y-pat)
                       (if (null? xr*)
                           (match-empty (vector-ref p 1) r)
                           (combine xr* r)))))
               ((atom) (and (equal? (vector-ref p 1) (strip e m*)) r))
               ((vector)
                (and (vector? e)
                     (match (vector->list e) (vector-ref p 1) m* s* ae* r)))
               (else (assertion-violation 'syntax-dispatch "invalid pattern" p)))))))
      (define match
        (lambda (e p m* s* ae* r)
          (cond
            ((not r) #f)
            ((eq? p '_) r)
            ((eq? p 'any) (cons (stx^ e m* s* ae*) r))
            ((stx? e)
             (and (not (top-marked? m*))
               (let-values (((m* s* ae*) (join-wraps m* s* ae* e)))
                 (match (stx-expr e) p m* s* ae* r))))
            ((annotation? e) 
             (match (annotation-expression e) p m* s* ae* r))
            (else (match* e p m* s* ae* r)))))
      (match e p '() '() '() '())))
  
  (define ellipsis?
    (lambda (x)
      (and (id? x) (free-id=? x (scheme-stx '...)))))

  (define underscore?
    (lambda (x)
      (and (id? x) (free-id=? x (scheme-stx '_)))))

  (define (verify-literals lits expr)
    (for-each
      (lambda (x) 
        (when (or (not (id? x)) (ellipsis? x) (underscore? x))
          (syntax-violation #f "invalid literal" expr x)))
      lits))

  (define syntax-case-transformer
    (let ()
      (define build-dispatch-call
        (lambda (pvars expr y r mr)
          (let ((ids (map car pvars))
                (levels (map cdr pvars)))
            (let ((labels (map gen-label ids))
                  (new-vars (map gen-lexical ids)))
              (let ((body
                     (chi-expr
                       (add-subst (make-full-rib ids labels) expr)
                       (append
                         (map (lambda (label var level)
                                (cons label (make-binding 'syntax (cons var level))))
                              labels new-vars (map cdr pvars))
                         r)
                       mr)))
                (build-application no-source
                  (build-primref no-source 'apply)
                  (list (build-lambda no-source new-vars body) y)))))))
      (define invalid-ids-error
        (lambda (id* e class)
          (let find ((id* id*) (ok* '()))
            (if (null? id*)
                (stx-error e) ; shouldn't happen
                (if (id? (car id*))
                    (if (bound-id-member? (car id*) ok*)
                        (syntax-error (car id*) "duplicate " class)
                        (find (cdr id*) (cons (car id*) ok*)))
                    (syntax-error (car id*) "invalid " class))))))
      (define gen-clause
        (lambda (x keys clauses r mr pat fender expr)
          (let-values (((p pvars) (convert-pattern pat keys)))
            (cond
              ((not (distinct-bound-ids? (map car pvars)))
               (invalid-ids-error (map car pvars) pat "pattern variable"))
              ((not (for-all (lambda (x) (not (ellipsis? (car x)))) pvars))
               (stx-error pat "misplaced ellipsis in syntax-case pattern"))
              (else
               (let ((y (gen-lexical 'tmp)))
                 (let ((test
                        (cond
                          ((eq? fender #t) y)
                          (else
                           (let ((call
                                  (build-dispatch-call
                                     pvars fender y r mr)))
                             (build-conditional no-source
                                (build-lexical-reference no-source y)
                                call
                                (build-data no-source #f)))))))
                    (let ((conseq
                           (build-dispatch-call pvars expr
                             (build-lexical-reference no-source y)
                             r mr)))
                      (let ((altern
                             (gen-syntax-case x keys clauses r mr)))
                        (build-application no-source
                          (build-lambda no-source (list y)
                            (build-conditional no-source test conseq altern))
                          (list
                            (build-application no-source
                              (build-primref no-source 'syntax-dispatch)
                              (list
                                (build-lexical-reference no-source x)
                                (build-data no-source p))))))))))))))
      (define gen-syntax-case
        (lambda (x keys clauses r mr)
          (if (null? clauses)
              (build-application no-source
                (build-primref no-source 'syntax-error)
                (list (build-lexical-reference no-source x)))
              (syntax-match (car clauses) ()
                ((pat expr)
                 (if (and (id? pat)
                          (not (bound-id-member? pat keys))
                          (not (ellipsis? pat)))
                     (if (free-id=? pat (scheme-stx '_))
                         (chi-expr expr r mr)
                         (let ((lab (gen-label pat))
                               (lex (gen-lexical pat)))
                           (let ((body
                                  (chi-expr
                                    (add-subst (make-full-rib (list pat) (list lab)) expr)
                                    (cons (cons lab (make-binding 'syntax (cons lex 0))) r)
                                    mr)))
                              (build-application no-source
                                (build-lambda no-source (list lex) body)
                                (list (build-lexical-reference no-source x))))))
                     (gen-clause x keys (cdr clauses) r mr pat #t expr)))
                ((pat fender expr)
                 (gen-clause x keys (cdr clauses) r mr pat fender expr))))))
      (lambda (e r mr)
        (syntax-match e ()
          ((_ expr (keys ...) clauses ...)
           (begin
             (verify-literals keys e)
             (let ((x (gen-lexical 'tmp)))
               (let ((body (gen-syntax-case x keys clauses r mr)))
                 (build-application no-source
                   (build-lambda no-source (list x) body)
                   (list (chi-expr expr r mr)))))))))))

  (define (ellipsis-map proc ls . ls*)
    (define who '...)
    (unless (list? ls) 
      (assertion-violation who "not a list" ls))
    (unless (null? ls*)
      (let ((n (length ls)))
        (for-each
          (lambda (x) 
            (unless (list? x) 
              (assertion-violation who "not a list" x))
            (unless (= (length x) n)
              (assertion-violation who "length mismatch" ls x)))
          ls*)))
    (apply map proc ls ls*))

  (define syntax-transformer
    (let ()
      (define gen-syntax
        (lambda (src e r maps ellipsis? vec?)
          (syntax-match e ()
            (dots (ellipsis? dots)
             (stx-error src "misplaced ellipsis in syntax form"))
            (id (id? id)
             (let* ((label (id->label e))
                    (b (label->binding label r)))
                 (if (eq? (binding-type b) 'syntax)
                     (let-values (((var maps)
                                   (let ((var.lev (binding-value b)))
                                     (gen-ref src (car var.lev) (cdr var.lev) maps))))
                       (values (list 'ref var) maps))
                     (values (list 'quote e) maps))))
            ((dots e) (ellipsis? dots)
             (if vec?
                 (stx-error src "misplaced ellipsis in syntax form")
                 (gen-syntax src e r maps (lambda (x) #f) #f)))
            ((x dots . y) (ellipsis? dots)
             (let f ((y y)
                     (k (lambda (maps)
                          (let-values (((x maps)
                                        (gen-syntax src x r
                                          (cons '() maps) ellipsis? #f)))
                            (if (null? (car maps))
                                (stx-error src
                                  "extra ellipsis in syntax form")
                                (values (gen-map x (car maps)) (cdr maps)))))))
               (syntax-match y ()
                 (() (k maps))
                 ((dots . y) (ellipsis? dots)
                  (f y
                     (lambda (maps)
                       (let-values (((x maps) (k (cons '() maps))))
                         (if (null? (car maps))
                             (stx-error src "extra ellipsis in syntax form")
                             (values (gen-mappend x (car maps)) (cdr maps)))))))
                 (_
                  (let-values (((y maps)
                                (gen-syntax src y r maps ellipsis? vec?)))
                    (let-values (((x maps) (k maps)))
                      (values (gen-append x y) maps)))))))
            ((x . y)
             (let-values (((xnew maps)
                           (gen-syntax src x r maps ellipsis? #f)))
               (let-values (((ynew maps)
                             (gen-syntax src y r maps ellipsis? vec?)))
                 (values (gen-cons e x y xnew ynew) maps))))
            (#(ls ...) 
             (let-values (((lsnew maps)
                           (gen-syntax src ls r maps ellipsis? #t)))
               (values (gen-vector e ls lsnew) maps)))
            (_ (values `(quote ,e) maps)))))
      (define gen-ref
        (lambda (src var level maps)
          (if (= level 0)
              (values var maps)
              (if (null? maps)
                  (stx-error src "missing ellipsis in syntax form")
                  (let-values (((outer-var outer-maps)
                                (gen-ref src var (- level 1) (cdr maps))))
                    (cond
                      ((assq outer-var (car maps)) =>
                       (lambda (b) (values (cdr b) maps)))
                      (else
                       (let ((inner-var (gen-lexical 'tmp)))
                         (values
                           inner-var
                           (cons
                             (cons (cons outer-var inner-var) (car maps))
                             outer-maps))))))))))
      (define gen-append
        (lambda (x y)
          (if (equal? y '(quote ())) x `(append ,x ,y))))
      (define gen-mappend
        (lambda (e map-env)
          `(apply (primitive append) ,(gen-map e map-env))))
      (define gen-map
        (lambda (e map-env)
          (let ((formals (map cdr map-env))
                (actuals (map (lambda (x) `(ref ,(car x))) map-env)))
            (cond
             ; identity map equivalence:
             ; (map (lambda (x) x) y) == y
              ((eq? (car e) 'ref)
               (car actuals))
             ; eta map equivalence:
             ; (map (lambda (x ...) (f x ...)) y ...) == (map f y ...)
              ((for-all
                 (lambda (x) (and (eq? (car x) 'ref) (memq (cadr x) formals)))
                 (cdr e))
               (let ((args (map (let ((r (map cons formals actuals)))
                                  (lambda (x) (cdr (assq (cadr x) r))))
                                (cdr e))))
                 `(map (primitive ,(car e)) . ,args)))
              (else (cons* 'map (list 'lambda formals e) actuals))))))
      (define gen-cons
        (lambda (e x y xnew ynew)
          (case (car ynew)
            ((quote)
             (if (eq? (car xnew) 'quote)
                 (let ((xnew (cadr xnew)) (ynew (cadr ynew)))
                   (if (and (eq? xnew x) (eq? ynew y))
                       `(quote ,e)
                       `(quote ,(cons xnew ynew))))
                 (if (null? (cadr ynew))
                     `(list ,xnew)
                     `(cons ,xnew ,ynew))))
            ((list) `(list ,xnew . ,(cdr ynew)))
            (else `(cons ,xnew ,ynew)))))
      (define gen-vector
        (lambda (e ls lsnew)
          (cond
            ((eq? (car lsnew) 'quote)
             (if (eq? (cadr lsnew) ls)
                 `(quote ,e)
                 `(quote #(,@(cadr lsnew)))))
            ((eq? (car lsnew) 'list)
             `(vector . ,(cdr lsnew)))
            (else `(list->vector ,lsnew)))))
      (define regen
        (lambda (x)
          (case (car x)
            ((ref)        (build-lexical-reference no-source (cadr x)))
            ((primitive)  (build-primref no-source (cadr x)))
            ((quote)      (build-data no-source (cadr x)))
            ((lambda)     (build-lambda no-source (cadr x) (regen (caddr x))))
            ((map)
             (let ((ls (map regen (cdr x))))
               (build-application no-source
                 (build-primref no-source 'ellipsis-map)
                 ls)))
            (else
             (build-application no-source
               (build-primref no-source (car x))
               (map regen (cdr x)))))))
      (lambda (e r mr)
      (syntax-match e ()
        ((_ x)
         (let-values (((e maps) (gen-syntax e x r '() ellipsis? #f)))
             (regen e)))))))
  
  (define core-macro-transformer
    (lambda (name)
      (case name
        ((quote)                  quote-transformer)
        ((lambda)                 lambda-transformer)
        ((typed-lambda)           typed-lambda-transformer)
        ((case-lambda)            case-lambda-transformer)
        ((typed-case-lambda)      typed-case-lambda-transformer)
        ((letrec)                 letrec-transformer)
        ((letrec*)                letrec*-transformer)
        ((if)                     if-transformer)
        ((foreign-call)           foreign-call-transformer)
        ((syntax-case)            syntax-case-transformer)
        ((syntax)                 syntax-transformer)
        ((type-descriptor)        type-descriptor-transformer)
        ((record-type-descriptor) record-type-descriptor-transformer)
        ((record-constructor-descriptor) record-constructor-descriptor-transformer)
        ((fluid-let-syntax)       fluid-let-syntax-transformer)
        (else (assertion-violation 
                'macro-transformer
                "BUG: cannot find transformer"
                name)))))
  
  (define file-options-macro
    (lambda (x)
      (define (valid-option? x)
        (and (id? x) (memq (id->sym x) '(no-fail no-create no-truncate))))
      (syntax-match x ()
        ((_ opt* ...)
         (and (for-all valid-option? opt*) 
			(file-options-spec (map id->sym opt*)))
         (bless `(quote ,(file-options-spec (map id->sym opt*))))))))

  (define symbol-macro
    (lambda (x set)
      (syntax-match x ()
        ((_ name)
         (and (id? name) (memq (id->sym name) set))
         (bless `(quote ,name))))))

  (define macro-transformer
    (lambda (x)
      (cond
        ((procedure? x) x)
        ((symbol? x)
         (case x
           ((define-record-type)    define-record-type-macro)
           ((define-struct)         define-struct-macro)
           ((include)               include-macro)
           ((cond)                  cond-macro)
           ((let)                   let-macro)
           ((do)                    do-macro)
           ((or)                    or-macro)
           ((and)                   and-macro)
           ((let*)                  let*-macro)
           ((let-values)            let-values-macro)
           ((let*-values)           let*-values-macro)
           ((syntax-rules)          syntax-rules-macro)
           ((quasiquote)            quasiquote-macro)
           ((quasisyntax)           quasisyntax-macro)
           ((with-syntax)           with-syntax-macro)
           ((when)                  when-macro)
           ((unless)                unless-macro)
           ((case)                  case-macro)
           ((identifier-syntax)     identifier-syntax-macro)
           ((time)                  time-macro)
           ((delay)                 delay-macro)
           ((assert)                assert-macro)
           ((endianness)            endianness-macro)
           ((guard)                 guard-macro)
           ((define-enumeration)    define-enumeration-macro)
           ((trace-lambda)          trace-lambda-macro)
           ((trace-define)          trace-define-macro)
           ((trace-let)             trace-let-macro)
           ((trace-define-syntax)   trace-define-syntax-macro)
           ((trace-let-syntax)      trace-let-syntax-macro)
           ((trace-letrec-syntax)   trace-letrec-syntax-macro)
           ((define-condition-type) define-condition-type-macro)
           ((parameterize)          parameterize-macro)
           ((include-into)          include-into-macro)
           ((eol-style)
            (lambda (x) 
              (symbol-macro x '(none lf cr crlf nel crnel ls))))
           ((error-handling-mode)         
            (lambda (x) 
              (symbol-macro x '(ignore raise replace))))
           ((buffer-mode)         
            (lambda (x) 
              (symbol-macro x '(none line block))))
           ((file-options)     file-options-macro)
           ((... => _ else unquote unquote-splicing
             unsyntax unsyntax-splicing 
             fields mutable immutable parent protocol
             sealed opaque nongenerative parent-rtd)
            incorrect-usage-macro)
           (else 
            (error 'macro-transformer "BUG: invalid macro" x))))
        (else 
         (error 'core-macro-transformer "BUG: invalid macro" x)))))
  
  (define (local-macro-transformer x)
    (car x))

  (define (do-macro-call transformer expr r rib)
    (define (return x)
      (let f ((x x))
        ;;; don't feed me cycles.
        (unless (stx? x)
          (cond
            ((pair? x) (f (car x)) (f (cdr x)))
            ((vector? x) (vector-for-each f x))
            ((symbol? x) 
             (syntax-violation #f 
               "raw symbol encountered in output of macro"
               expr x)))))
      (add-mark (gen-mark) rib x expr))
    (let ((x (transformer (add-mark anti-mark #f expr #f))))
      (if (procedure? x) 
          (return 
            (x (lambda (id)
                 (unless (id? id) 
                   (assertion-violation 'rho "not an identifier" id))
                 (let ([label (id->label id)])
                   (let ([binding (label->binding label r)])
                     (case (car binding)
                       [(local-ctv) (cadr binding)]
                       [(global-ctv) 
                        (let ([lib (cadr binding)]
                              [loc (cddr binding)])
                          (unless (eq? lib '*interaction*)
                            (visit-library lib))
                          (symbol-value loc))]
                       [else #f]))))))
          (return x))))

  ;;; chi procedures
  (define (chi-macro p e r rib)
    (do-macro-call (macro-transformer p) e r rib))
  
  (define (chi-local-macro p e r rib)
    (do-macro-call (local-macro-transformer p) e r rib))
  
  (define (chi-global-macro p e r rib)
    (let ((lib (car p))
          (loc (cdr p)))
      (unless (eq? lib '*interaction*)
        (visit-library lib))
      (let ((x (symbol-value loc)))
        (let ((transformer
               (cond
                 ((procedure? x) x)
                 ((and (pair? x) 
                       (eq? (car x) 'macro!)
                       (procedure? (cdr x)))
                  (cdr x))
                 (else (assertion-violation 'chi-global-macro
                          "BUG: not a procedure" x)))))
          (do-macro-call transformer e r rib)))))
  
  (define chi-expr*
    (lambda (e* r mr)
      ;;; expand left to right
      (cond
        ((null? e*) '())
        (else
         (let ((e (chi-expr (car e*) r mr)))
           (cons e (chi-expr* (cdr e*) r mr)))))))

  (define chi-application
    (lambda (e r mr)
      (syntax-match e  ()
        ((rator rands ...)
         (let ((rator (chi-expr rator r mr)))
           (build-application (syntax-annotation e)
             rator
             (chi-expr* rands r mr)))))))

  (define chi-expr
    (lambda (e r mr)
      (let-values (((type value kwd) (syntax-type e r)))
        (case type
          ((core-macro)
           (let ((transformer (core-macro-transformer value)))
             (transformer e r mr)))
          ((global)
           (let* ((lib (car value))
                  (loc (cdr value)))
             ((inv-collector) lib)
             (build-global-reference (syntax-annotation e) loc)))
          ((core-prim)
           (let ((name value))
             (build-primref no-source name)))
          ((call) (chi-application e r mr))
          ((lexical)
           (let ((lex (lexical-var value)))
             (build-lexical-reference (syntax-annotation e) lex)))
          ((global-macro global-macro!)
           (chi-expr (chi-global-macro value e r #f) r mr))
          ((local-macro local-macro!) 
           (chi-expr (chi-local-macro value e r #f) r mr))
          ((macro macro!)
           (chi-expr (chi-macro value e r #f) r mr))
          ((constant)
           (let ((datum value))
             (build-data (syntax-annotation e) datum)))
          ((set!) (chi-set! e r mr))
          ((begin)
           (syntax-match e ()
             ((_ x x* ...)
              (build-sequence no-source
                (chi-expr* (cons x x*) r mr)))))
          ((stale-when)
           (syntax-match e ()
             ((_ guard x x* ...)
              (begin
                (handle-stale-when guard mr)
                (build-sequence no-source
                  (chi-expr* (cons x x*) r mr))))))
          ((let-syntax letrec-syntax)
           (syntax-match e ()
             ((_ ((xlhs* xrhs*) ...) xbody xbody* ...)
              (unless (valid-bound-ids? xlhs*)
                (stx-error e "invalid identifiers"))
              (let* ((xlab* (map gen-label xlhs*))
                     (xrib (make-full-rib xlhs* xlab*))
                     (xb* (map (lambda (x)
                                (make-eval-transformer
                                  (expand-transformer
                                    (if (eq? type 'let-syntax)
                                        x
                                        (add-subst xrib x))
                                    mr)))
                              xrhs*)))
                (build-sequence no-source
                  (chi-expr*
                    (map (lambda (x) (add-subst xrib x)) (cons xbody xbody*))
                    (append (map cons xlab* xb*) r)
                    (append (map cons xlab* xb*) mr)))))))
          ((displaced-lexical)
           (stx-error e "identifier out of context"))
          ((syntax) (stx-error e "reference to pattern variable outside a syntax form"))
          ((define define-syntax define-fluid-syntax module import library)
           (stx-error e 
             (string-append 
               (case type
                 ((define)        "a definition")
                 ((define-syntax) "a define-syntax")
                 ((define-fluid-syntax) "a define-fluid-syntax")
                 ((module)        "a module definition")
                 ((library)       "a library definition")
                 ((import)        "an import declaration")
                 ((export)        "an export declaration")
                 (else            "a non-expression"))
               " was found where an expression was expected")))
          ((mutable) 
           (if (and (pair? value) (let ((lib (car value))) (eq? lib '*interaction*)))
               (let ((loc (cdr value))) (build-global-reference (syntax-annotation e) loc))
               (stx-error e "attempt to reference an unexportable variable")))
          (else
           ;(assertion-violation 'chi-expr "invalid type " type (strip e '()))
           (stx-error e "invalid expression"))))))

  (define chi-set!
    (lambda (e r mr)
      (syntax-match e ()
        ((_ x v) (id? x)
         (let-values (((type value kwd) (syntax-type x r)))
           (case type
             ((lexical)
              (set-lexical-mutable! value #t)
              (build-lexical-assignment (syntax-annotation e)
                (lexical-var value)
                (chi-expr v r mr)))
             ((core-prim)
              (stx-error e "cannot modify imported core primitive"))
             ((global)
              (stx-error e "attempt to modify an immutable binding"))
             ((global-macro!)
              (chi-expr (chi-global-macro value e r #f) r mr))
             ((local-macro!)
              (chi-expr (chi-local-macro value e r #f) r mr))
             ((mutable) 
              (if (and (pair? value) (let ((lib (car value))) (eq? lib '*interaction*)))
                  (let ([loc (cdr value)])
                    (build-global-assignment (syntax-annotation e) loc 
                      (chi-expr v r mr)))
                  (stx-error e "attempt to modify an unexportable variable")))
             (else (stx-error e))))))))
  
  (define (verify-formals fmls stx)
    (syntax-match fmls ()
      ((x* ...)
       (unless (valid-bound-ids? x*)
         (invalid-fmls-error stx fmls)))
      ((x* ... . x)
       (unless (valid-bound-ids? (cons x x*))
         (invalid-fmls-error stx fmls)))
      (_ (stx-error stx "invalid syntax"))))

  (define chi-lambda-clause
    (lambda (stx fmls body* r mr)
      (syntax-match fmls ()
        ((x* ...)
         (begin
           (verify-formals fmls stx)
           (let ((lex* (map gen-lexical x*))
                 (lab* (map gen-label x*)))
             (values
               lex*
               (chi-internal
                 (add-subst (make-full-rib x* lab*) body*)
                 (add-lexicals lab* lex* r)
                 mr)))))
        ((x* ... . x)
         (begin
           (verify-formals fmls stx)
           (let ((lex* (map gen-lexical x*)) (lab* (map gen-label x*))
                 (lex (gen-lexical x)) (lab (gen-label x)))
             (values
               (append lex* lex)
               (chi-internal
                 (add-subst
                   (make-full-rib (cons x x*) (cons lab lab*))
                   body*)
                 (add-lexicals (cons lab lab*) (cons lex lex*) r)
                 mr)))))
        (_ (stx-error fmls "invalid syntax")))))
  
  (define chi-lambda-clause*
    (lambda (stx fmls* body** r mr)
      (cond
        ((null? fmls*) (values '() '()))
        (else
         (let-values (((a b)
                       (chi-lambda-clause stx (car fmls*) (car body**) r mr)))
           (let-values (((a* b*)
                         (chi-lambda-clause* stx (cdr fmls*) (cdr body**) r mr)))
             (values (cons a a*) (cons b b*))))))))

  (define (chi-defun x r mr)
    (syntax-match x ()
      [(_ (ctxt . fmls) . body*)
      (let-values (((fmls body)
                    (chi-lambda-clause fmls fmls body* r mr)))
          (build-lambda (syntax-annotation x) fmls body))]))

  (define chi-rhs
    (lambda (rhs r mr)
      (case (car rhs)
        ((defun) (chi-defun (cdr rhs) r mr))
        ((expr)
         (let ((expr (cdr rhs)))
           (chi-expr expr r mr)))
        ((top-expr)
         (let ((expr (cdr rhs)))
           (build-sequence no-source
             (list (chi-expr expr r mr)
                   (build-void)))))
        (else (assertion-violation 'chi-rhs "BUG: invalid rhs" rhs)))))

  (define (expand-interaction-rhs*/init* lhs* rhs* init* r mr)
    (let f ((lhs* lhs*) (rhs* rhs*))
      (cond
        ((null? lhs*)
         (map (lambda (x) (chi-expr x r mr)) init*))
        (else
         (let ((lhs (car lhs*)) (rhs (car rhs*)))
           (case (car rhs)
             ((defun) 
              (let ((rhs (chi-defun (cdr rhs) r mr)))
                (cons
                  (build-global-assignment no-source lhs rhs)
                  (f (cdr lhs*) (cdr rhs*)))))
             ((expr) 
              (let ((rhs (chi-expr (cdr rhs) r mr)))
                (cons
                  (build-global-assignment no-source lhs rhs)
                  (f (cdr lhs*) (cdr rhs*)))))
             ((top-expr) 
              (let ((e (chi-expr (cdr rhs) r mr)))
                (cons e (f (cdr lhs*) (cdr rhs*)))))
             (else (error 'expand-interaction "invallid" rhs))))))))

  (define chi-rhs*
    (lambda (rhs* r mr)
      (let f ((ls rhs*))
        (cond ;;; chi-rhs in order
          ((null? ls) '())
          (else
           (let ((a (chi-rhs (car ls) r mr)))
             (cons a (f (cdr ls)))))))))

  (define find-bound=?
    (lambda (x lhs* rhs*)
      (cond
        ((null? lhs*) #f)
        ((bound-id=? x (car lhs*)) (car rhs*))
        (else (find-bound=? x (cdr lhs*) (cdr rhs*))))))

  (define (find-dups ls)
    (let f ((ls ls) (dups '()))
      (cond
        ((null? ls) dups)
        ((find-bound=? (car ls) (cdr ls) (cdr ls)) =>
         (lambda (x) (f (cdr ls) (cons (list (car ls) x) dups))))
        (else (f (cdr ls) dups)))))
  
  (define chi-internal
    (lambda (e* r mr)
      (let ((rib (make-empty-rib)))
        (let-values (((e* r mr lex* rhs* mod** kwd* _exp*)
                      (chi-body* 
                        (map (lambda (x) (add-subst rib x)) (syntax->list e*))
                         r mr '() '() '() '() '() rib #f #t)))
           (when (null? e*)
             (stx-error e* "no expression in body"))
           (let* ((init* 
                   (chi-expr* (append (apply append (reverse mod**)) e*) r mr))
                  (rhs* (chi-rhs* rhs* r mr)))
             (build-letrec* no-source
                (reverse lex*) (reverse rhs*)
                (build-sequence no-source init*)))))))

  (define parse-module
    (lambda (e)
      (syntax-match e ()
        ((_ (export* ...) b* ...)
         (begin
           (unless (for-all id? export*)
             (stx-error e "module exports must be identifiers"))
           (values #f (list->vector export*) b*)))
        ((_ name (export* ...) b* ...)
         (begin
           (unless (id? name)
             (stx-error e "module name must be an identifier"))
           (unless (for-all id? export*)
             (stx-error e "module exports must be identifiers"))
           (values name (list->vector export*) b*))))))

  (define-record module-interface (first-mark exp-id-vec exp-lab-vec))

  (define (module-interface-exp-id* iface id)
    (define (diff-marks ls x) 
      (when (null? ls) (error 'diff-marks "BUG: should not happen"))
      (let ((a (car ls)))
        (if (eq? a x)
            '()
            (cons a (diff-marks (cdr ls) x)))))
    (let ((diff
           (diff-marks (stx-mark* id) (module-interface-first-mark iface)))
          (id-vec (module-interface-exp-id-vec iface)))
      (if (null? diff)
          id-vec
          (vector-map
            (lambda (x)
              (make-stx (stx-expr x) (append diff (stx-mark* x)) '() '()))
            id-vec))))

  (define (syntax-transpose object base-id new-id)
    (define who 'syntax-transpose)
    (define (err msg . args) (apply assertion-violation who msg args))
    (define (split s*)
      (cond
        [(eq? (car s*) 'shift) 
         (values (list 'shift) (cdr s*))]
        [else
         (let-values ([(s1* s2*) (split (cdr s*))])
           (values (cons (car s*) s1*) s2*))]))
    (define (final s*)
      (cond
        [(or (null? s*) (eq? (car s*) 'shift)) '()]
        [else (cons (car s*) (final (cdr s*)))]))
    (define (diff m m* s* ae*)
      (if (null? m*)
          (err "unmatched identifiers" base-id new-id)
          (let ([m1 (car m*)])
            (if (eq? m m1)
                (values '() (final s*) '())
                (let-values ([(s1* s2*) (split s*)])
                (let-values ([(nm* ns* nae*)
                              (diff m (cdr m*) s2* (cdr ae*))])
                  (values (cons m1 nm*) 
                          (append s1* ns*)
                          (cons (car ae*) nae*))))))))
    (unless (id? base-id) (err "not an identifier" base-id))
    (unless (id? new-id) (err "not an identifier" new-id))
    (unless (free-identifier=? base-id new-id)
      (err "not the same identifier" base-id new-id))
    (let-values ([(m* s* ae*)
                  (diff (car (stx-mark* base-id))
                    (stx-mark* new-id)
                    (stx-subst* new-id)
                    (stx-ae* new-id))])
      (if (and (null? m*) (null? s*))
          object
          (mkstx object m* s* ae*))))

  (define chi-internal-module
    (lambda (e r mr lex* rhs* mod** kwd*)
      (let-values (((name exp-id* e*) (parse-module e)))
        (let* ((rib (make-empty-rib))
               (e* (map (lambda (x) (add-subst rib x)) (syntax->list e*))))
          (let-values (((e* r mr lex* rhs* mod** kwd* _exp*)
                        (chi-body* e* r mr lex* rhs* mod** kwd* '() rib #f #t)))
              (let ((exp-lab*
                     (vector-map
                       (lambda (x)
                         (or (id->label 
                               (make-stx (id->sym x) (stx-mark* x)
                                 (list rib)
                                 '()))
                             (stx-error x "cannot find module export")))
                      exp-id*))
                    (mod** (cons e* mod**)))
                (if (not name) ;;; explicit export
                    (values lex* rhs* exp-id* exp-lab* r mr mod** kwd*)
                    (let ((lab (gen-label 'module))
                          (iface 
                           (make-module-interface 
                             (car (stx-mark* name))
                             (vector-map 
                               (lambda (x) 
                                 (make-stx (stx-expr x) (stx-mark* x) '() '()))
                               exp-id*)
                             exp-lab*)))
                      (values lex* rhs*
                              (vector name) ;;; FIXME: module cannot
                              (vector lab)  ;;;  export itself yet
                              (cons (cons lab (cons '$module iface)) r)
                              (cons (cons lab (cons '$module iface)) mr)
                              mod** kwd*)))))))))

  (define chi-body*
    (lambda (e* r mr lex* rhs* mod** kwd* exp* rib mix? sd?)
      (cond
        ((null? e*) (values e* r mr lex* rhs* mod** kwd* exp*))
        (else
         (let ((e (car e*)))
           (let-values (((type value kwd) (syntax-type e r)))
             (let ((kwd* (if (id? kwd) (cons kwd kwd*) kwd*)))
               (case type
                 ((define)
                  (let-values (((id rhs) (parse-define e)))
                    (when (bound-id-member? id kwd*)
                      (stx-error e "cannot redefine keyword"))
                    (let-values (((lab lex) (gen-define-label+loc id rib sd?)))
                      (extend-rib! rib id lab sd?)
                      (chi-body* (cdr e*)
                         (add-lexical lab lex r) mr
                         (cons lex lex*) (cons rhs rhs*)
                         mod** kwd* exp* rib mix? sd?))))
                 ((define-syntax)
                  (let-values (((id rhs) (parse-define-syntax e)))
                    (when (bound-id-member? id kwd*)
                      (stx-error e "cannot redefine keyword"))
                    (let* ((lab (gen-define-label id rib sd?))
                           (expanded-rhs (expand-transformer rhs mr)))
                        (extend-rib! rib id lab sd?)
                        (let ((b (make-eval-transformer expanded-rhs)))
                          (chi-body* (cdr e*)
                             (cons (cons lab b) r) (cons (cons lab b) mr)
                             lex* rhs* mod** kwd* exp* rib
                             mix? sd?)))))
                 ((define-fluid-syntax)
                  (let-values (((id rhs) (parse-define-syntax e)))
                    (when (bound-id-member? id kwd*)
                      (stx-error e "cannot redefine keyword"))
                    (let* ((lab (gen-define-label id rib sd?))
                           (flab (gen-define-label id rib sd?))
                           (expanded-rhs (expand-transformer rhs mr)))
                        (extend-rib! rib id lab sd?)
                        (let ((b (make-eval-transformer expanded-rhs)))
                          (let ([t1 (cons lab (cons '$fluid flab))]
                                [t2 (cons flab b)])
                            (chi-body* (cdr e*)
                               (cons* t1 t2 r) (cons* t1 t2 mr)
                               lex* rhs* mod** kwd* exp* rib
                               mix? sd?))))))
                 ((let-syntax letrec-syntax)
                  (syntax-match e ()
                    ((_ ((xlhs* xrhs*) ...) xbody* ...)
                     (unless (valid-bound-ids? xlhs*)
                       (stx-error e "invalid identifiers"))
                     (let* ((xlab* (map gen-label xlhs*))
                            (xrib (make-full-rib xlhs* xlab*))
                            (xb* (map (lambda (x)
                                       (make-eval-transformer
                                         (expand-transformer
                                           (if (eq? type 'let-syntax)
                                               x
                                               (add-subst xrib x))
                                           mr)))
                                     xrhs*)))
                       (chi-body*
                         (append (map (lambda (x) (add-subst xrib x)) xbody*) (cdr e*))
                         (append (map cons xlab* xb*) r)
                         (append (map cons xlab* xb*) mr)
                         lex* rhs* mod** kwd* exp* rib 
                         mix? sd?)))))
                 ((begin)
                  (syntax-match e ()
                    ((_ x* ...)
                     (chi-body* (append x* (cdr e*))
                        r mr lex* rhs* mod** kwd* exp* rib
                        mix? sd?))))
                 ((stale-when)
                  (syntax-match e ()
                    ((_ guard x* ...) 
                     (begin
                       (handle-stale-when guard mr)
                       (chi-body* (append x* (cdr e*))
                          r mr lex* rhs* mod** kwd* exp* rib
                          mix? sd?)))))
                 ((global-macro global-macro!)
                  (chi-body* 
                    (cons (chi-global-macro value e r rib) (cdr e*))
                    r mr lex* rhs* mod** kwd* exp* rib mix? sd?))
                 ((local-macro local-macro!)
                  (chi-body* 
                    (cons (chi-local-macro value e r rib) (cdr e*))
                    r mr lex* rhs* mod** kwd* exp* rib mix? sd?))
                 ((macro macro!)
                  (chi-body* 
                    (cons (chi-macro value e r rib) (cdr e*))
                    r mr lex* rhs* mod** kwd* exp* rib mix? sd?))
                 ((module)
                  (let-values (((lex* rhs* m-exp-id* m-exp-lab* r mr mod** kwd*)
                                (chi-internal-module e r mr lex* rhs* mod** kwd*)))
                    (vector-for-each
                      (lambda (id lab) (extend-rib! rib id lab sd?))
                      m-exp-id* m-exp-lab*)
                    (chi-body* (cdr e*) r mr lex* rhs* mod** kwd*
                               exp* rib mix? sd?)))
                 ((library) 
                  (library-expander (stx->datum e))
                  (chi-body* (cdr e*) r mr lex* rhs* mod** kwd* exp*
                             rib mix? sd?))
                 ((export)
                  (syntax-match e ()
                    ((_ exp-decl* ...) 
                     (chi-body* (cdr e*) r mr lex* rhs* mod** kwd* 
                                (append exp-decl* exp*) rib
                                mix? sd?))))
                 ((import)
                  (let ()
                    (define (module-import? e)
                      (syntax-match e ()
                        ((_ id) (id? id) #t)
                        ((_ imp* ...) #f)
                        (_ (stx-error e "malformed import form"))))
                    (define (module-import e r)
                      (syntax-match e ()
                        ((_ id) (id? id)
                         (let-values (((type value kwd) (syntax-type id r)))
                           (case type
                             (($module)
                              (let ((iface value))
                                (values 
                                  (module-interface-exp-id* iface id)
                                  (module-interface-exp-lab-vec iface))))
                             (else (stx-error e "invalid import")))))))
                    (define (library-import e) 
                      (syntax-match e ()
                        ((ctxt imp* ...)
                         (let-values (((subst-names subst-labels)
                                       (parse-import-spec* 
                                        (syntax->datum imp*))))
                           (values 
                             (vector-map 
                               (lambda (name) 
                                 (datum->stx ctxt name))
                               subst-names)
                             subst-labels)))
                        (_ (stx-error e "invalid import form"))))
                    (let-values (((id* lab*) 
                                  (if (module-import? e) 
                                      (module-import e r)
                                      (library-import e))))
                      (vector-for-each
                        (lambda (id lab) (extend-rib! rib id lab sd?))
                        id* lab*))
                    (chi-body* (cdr e*) r mr lex* rhs* mod** kwd*
                               exp* rib mix? sd?)))
                 (else
                  (if mix?
                      (chi-body* (cdr e*) r mr
                          (cons (gen-lexical 'dummy) lex*)
                          (cons (cons 'top-expr e) rhs*)
                          mod** kwd* exp* rib #t sd?)
                      (values e* r mr lex* rhs* mod** kwd* exp*)))))))))))
  
  (define (expand-transformer expr r)
    (let ((rtc (make-collector)))
      (let ((expanded-rhs
             (parameterize ((inv-collector rtc)
                            (vis-collector (lambda (x) (values))))
                 (chi-expr expr r r))))
        (for-each
          (let ((mark-visit (vis-collector)))
            (lambda (x)
              (invoke-library x)
              (mark-visit x)))
          (rtc))
        expanded-rhs)))

  (define (parse-exports exp*)
    (let f ((exp* exp*) (int* '()) (ext* '()))
      (cond
        ((null? exp*)
         (unless (valid-bound-ids? ext*)
           (syntax-violation 'export "invalid exports" 
              (find-dups ext*)))
         (values (map syntax->datum ext*) int*))
        (else
         (syntax-match (car exp*) ()
           ((rename (i* e*) ...)
            (begin
              (unless (and (eq? (syntax->datum rename) 'rename)
                           (for-all id? i*)
                           (for-all id? e*))
                (syntax-violation 'export "invalid export specifier" (car exp*)))
              (f (cdr exp*) (append i* int*) (append e* ext*))))
           (ie
            (begin
              (unless (id? ie)
                (syntax-violation 'export "invalid export" ie))
              (f (cdr exp*) (cons ie int*) (cons ie ext*)))))))))

  ;;; given a library name, like (foo bar (1 2 3)), 
  ;;; returns the identifiers and the version of the library
  ;;; as (foo bar) (1 2 3).  
  (define (parse-library-name spec)
    (define (parse x)
      (syntax-match x ()
        (((v* ...)) 
         (for-all 
           (lambda (x) 
             (let ((x (syntax->datum x)))
               (and (integer? x) (exact? x))))
           v*)
         (values '() (map syntax->datum v*)))
        ((x . rest) (symbol? (syntax->datum x))
         (let-values (((x* v*) (parse rest)))
           (values (cons (syntax->datum x) x*) v*)))
        (() (values '() '()))
        (_ (stx-error spec "invalid library name"))))
    (let-values (((name* ver*) (parse spec)))
      (when (null? name*) (stx-error spec "empty library name"))
      (values name* ver*)))

  ;;; given a library form, returns the name part, the export 
  ;;; specs, import specs and the body of the library.  
  (define parse-library
    (lambda (e)
      (syntax-match e ()
        ((library (name* ...)
            (export exp* ...)
            (import imp* ...)
            b* ...)
         (and (eq? (syntax->datum export) 'export) 
              (eq? (syntax->datum import) 'import) 
              (eq? (syntax->datum library) 'library))
         (values name* exp* imp* b*))
        (_ (stx-error e "malformed library")))))

  ;;; given a list of import-specs, return a subst and the list of
  ;;; libraries that were imported.
  ;;; Example: given ((rename (only (foo) x z) (x y)) (only (bar) q))
  ;;; returns: ((z . z$label) (y . x$label) (q . q$label))
  ;;;     and  (#<library (foo)> #<library (bar)>)
  
  (define (parse-import-spec* imp*)
    (define (idsyn? x) (symbol? (syntax->datum x)))
    (define (dup-error name)
      (syntax-violation 'import "two imports with different bindings" name))
    (define (merge-substs s subst)
      (define (insert-to-subst a subst)
        (let ((name (car a)) (label (cdr a)))
          (cond
            ((assq name subst) =>
             (lambda (x)
               (cond
                 ((eq? (cdr x) label) subst)
                 (else (dup-error name)))))
            (else
             (cons a subst)))))
      (cond
        ((null? s) subst)
        (else
         (insert-to-subst (car s)
           (merge-substs (cdr s) subst)))))
    (define (exclude* sym* subst)
      (define (exclude sym subst)
        (cond
          ((null? subst)
           (syntax-violation 'import "cannot rename unbound identifier" sym))
          ((eq? sym (caar subst))
           (values (cdar subst) (cdr subst)))
          (else
           (let ((a (car subst)))
             (let-values (((old subst) (exclude sym (cdr subst))))
               (values old (cons a subst)))))))
      (cond
        ((null? sym*) (values '() subst))
        (else
         (let-values (((old subst) (exclude (car sym*) subst)))
           (let-values (((old* subst) (exclude* (cdr sym*) subst)))
             (values (cons old old*) subst))))))
    (define (find* sym* subst)
      (map (lambda (x)
             (cond
               ((assq x subst) => cdr)
               (else (syntax-violation 'import "cannot find identifier" x))))
           sym*))
    (define (rem* sym* subst)
      (let f ((subst subst))
        (cond
          ((null? subst) '())
          ((memq (caar subst) sym*) (f (cdr subst)))
          (else (cons (car subst) (f (cdr subst)))))))
    (define (remove-dups ls)
      (cond
        ((null? ls) '())
        ((memq (car ls) (cdr ls)) (remove-dups (cdr ls)))
        (else (cons (car ls) (remove-dups (cdr ls))))))
    (define (parse-library-name spec)
      (define (subversion? x) 
        (let ((x (syntax->datum x)))
          (and (integer? x) (exact? x) (>= x 0))))
      (define (subversion-pred x*) 
        (syntax-match x* ()
          (n (subversion? n)
           (lambda (x) (= x (syntax->datum n))))
          ((p? sub* ...) (eq? (syntax->datum p?) 'and)
           (let ((p* (map subversion-pred sub*)))
             (lambda (x) 
               (for-all (lambda (p) (p x)) p*))))
          ((p? sub* ...) (eq? (syntax->datum p?) 'or)
           (let ((p* (map subversion-pred sub*)))
             (lambda (x) 
               (exists (lambda (p) (p x)) p*))))
          ((p? sub) (eq? (syntax->datum p?) 'not)
           (let ((p (subversion-pred sub)))
             (lambda (x) 
               (not (p x)))))
          ((p? n) 
           (and (eq? (syntax->datum p?) '<=) (subversion? n))
           (lambda (x) (<= x (syntax->datum n))))
          ((p? n) 
           (and (eq? (syntax->datum p?) '>=) (subversion? n))
           (lambda (x) (>= x (syntax->datum n))))
          (_ (syntax-violation 'import "invalid sub-version spec" spec x*))))
      (define (version-pred x*)
        (syntax-match x* ()
          (() (lambda (x) #t))
          ((c ver* ...) (eq? (syntax->datum c) 'and)
           (let ((p* (map version-pred ver*)))
             (lambda (x) 
               (for-all (lambda (p) (p x)) p*))))
          ((c ver* ...) (eq? (syntax->datum c) 'or)
           (let ((p* (map version-pred ver*)))
             (lambda (x) 
               (exists (lambda (p) (p x)) p*))))
          ((c ver) (eq? (syntax->datum c) 'not)
           (let ((p (version-pred ver)))
             (lambda (x) (not (p x)))))
          ((sub* ...) 
           (let ((p* (map subversion-pred sub*)))
             (lambda (x)
               (let f ((p* p*) (x x))
                 (cond
                   ((null? p*) #t)
                   ((null? x) #f)
                   (else 
                    (and ((car p*) (car x))
                         (f (cdr p*) (cdr x)))))))))
          (_ (syntax-violation 'import "invalid version spec" spec x*))))
      (let f ((x spec))
        (syntax-match x ()
          (((version-spec* ...)) 
           (values '() (version-pred version-spec*)))
          ((x . x*) (idsyn? x)
           (let-values (((name pred) (f x*)))
             (values (cons (syntax->datum x) name) pred)))
          (() (values '() (lambda (x) #t)))
          (_ (stx-error spec "invalid import spec")))))
    (define (import-library spec*)
      (let-values (((name pred) (parse-library-name spec*)))
        (when (null? name) 
          (syntax-violation 'import "empty library name" spec*))
        (let ((lib (find-library-by-name name)))
          (unless lib
            (syntax-violation 'import 
               "cannot find library with required name"
               name))
          (unless (pred (library-version lib))
            (syntax-violation 'import 
               "library does not satisfy version specification"
               spec* lib))
          ((imp-collector) lib)
          (library-subst lib))))
    (define (get-import spec)
      (syntax-match spec ()
        ((x x* ...)
         (not (memq (syntax->datum x) '(for rename except only prefix library)))
         (import-library (cons x x*)))
        ((rename isp (old* new*) ...)
         (and (eq? (syntax->datum rename) 'rename) 
              (for-all idsyn? old*) 
              (for-all idsyn? new*))
         (let ((subst (get-import isp))
               (old* (map syntax->datum old*))
               (new* (map syntax->datum new*)))
           ;;; rewrite this to eliminate find* and rem* and merge
           (let ((old-label* (find* old* subst)))
             (let ((subst (rem* old* subst)))
               ;;; FIXME: make sure map is valid
               (merge-substs (map cons new* old-label*) subst)))))
        ((except isp sym* ...) 
         (and (eq? (syntax->datum except) 'except) (for-all idsyn? sym*))
         (let ((subst (get-import isp)))
           (rem* (map syntax->datum sym*) subst)))
        ((only isp sym* ...)
         (and (eq? (syntax->datum only) 'only) (for-all idsyn? sym*))
         (let ((subst (get-import isp))
               (sym* (map syntax->datum sym*)))
           (let ((sym* (remove-dups sym*)))
             (let ((lab* (find* sym* subst)))
               (map cons sym* lab*)))))
        ((prefix isp p) 
         (and (eq? (syntax->datum prefix) 'prefix) (idsyn? p))
         (let ((subst (get-import isp))
               (prefix (symbol->string (syntax->datum p))))
           (map
             (lambda (x)
               (cons
                 (string->symbol
                   (string-append prefix
                     (symbol->string (car x))))
                 (cdr x)))
             subst)))
        ((library (spec* ...)) (eq? (syntax->datum library) 'library)
         (import-library spec*))
        ((for isp . rest)
         (eq? (syntax->datum for) 'for)
         (get-import isp))
        (spec (syntax-violation 'import "invalid import spec" spec))))
    (define (add-imports! imp h)
      (let ((subst (get-import imp)))
        (for-each
          (lambda (x) 
            (let ((name (car x)) (label (cdr x)))
              (cond
                ((hashtable-ref h name #f) =>
                 (lambda (l)
                   (unless (eq? l label) 
                     (dup-error name))))
                (else 
                 (hashtable-set! h name label)))))
          subst)))
    (let f ((imp* imp*) (h (make-eq-hashtable)))
      (cond
        ((null? imp*) 
         (hashtable-entries h))
        (else
         (add-imports! (car imp*) h)
         (f (cdr imp*) h)))))

  ;;; a top rib is constructed as follows:
  ;;; given a subst: name* -> label*,
  ;;; generate a rib containing:
  ;;;  - name* as the rib-sym*,
  ;;;  - a list of top-mark* as the rib-mark**
  ;;;  - label* as the rib-label*
  ;;; so, a name in a top rib maps to its label if and only if
  ;;; its set of marks is top-mark*.
  (define (make-top-rib names labels)
    (let ((rib (make-cache-rib)))
      (vector-for-each
        (lambda (name label)
          (unless (symbol? name) 
            (error 'make-top-rib "BUG: not a symbol" name))
          (extend-rib/nc! rib (make-stx name top-mark* '() '()) label))
        names labels)
      rib))

  (define (make-collector)
    (let ((ls '()))
      (case-lambda
        (() ls)
        ((x) 
         (unless (eq? x '*interaction*) 
           (set! ls (set-cons x ls)))))))

  (define inv-collector
    (make-parameter
      (lambda args
        (assertion-violation 'inv-collector "BUG: not initialized"))
      (lambda (x)
        (unless (procedure? x)
          (assertion-violation 'inv-collector "BUG: not a procedure" x))
        x)))
  
  (define vis-collector
    (make-parameter
      (lambda args
        (assertion-violation 'vis-collector "BUG: not initialized"))
      (lambda (x)
        (unless (procedure? x)
          (assertion-violation 'vis-collector "BUG: not a procedure" x))
        x)))
  
  (define imp-collector
    (make-parameter
      (lambda args
        (assertion-violation 'imp-collector "BUG: not initialized"))
      (lambda (x)
        (unless (procedure? x)
          (assertion-violation 'imp-collector "BUG: not a procedure" x))
        x)))


  (define chi-library-internal
    (lambda (e* rib mix?)
      (let-values (((e* r mr lex* rhs* mod** _kwd* exp*)
                    (chi-body* e* '() '() '() '() '() '() '() rib mix? #t)))
        (values (append (apply append (reverse mod**)) e*)
           r mr (reverse lex*) (reverse rhs*) exp*))))
  

  (define chi-interaction-expr
    (lambda (e rib r)
      (let-values (((e* r mr lex* rhs* mod** _kwd* _exp*)
                    (chi-body* (list e) r r '() '() '() '() '() rib
                               #t #f)))
        (let ((e* (expand-interaction-rhs*/init* 
                    (reverse lex*) (reverse rhs*) 
                    (append (apply append (reverse mod**)) e*)
                    r mr)))
          (let ((e (cond
                     ((null? e*) (build-void))
                     ((null? (cdr e*)) (car e*))
                     (else (build-sequence no-source e*)))))
             (values e r))))))

  (define library-body-expander
    (lambda (name main-exp* imp* b* mix?)
      (define itc (make-collector))
      (parameterize ((imp-collector itc)
                     (top-level-context #f))
        (let-values (((subst-names subst-labels)
                      (parse-import-spec* imp*)))
          (let ((rib (make-top-rib subst-names subst-labels)))
            (define (wrap x) (make-stx x top-mark* (list rib) '()))
            (let ((b* (map wrap b*))
                  (rtc (make-collector))
                  (vtc (make-collector)))
              (parameterize ((inv-collector rtc)
                             (vis-collector vtc))
                (let-values (((init* r mr lex* rhs* internal-exp*)
                              (chi-library-internal b* rib mix?)))
                  (let-values (((exp-name* exp-id*)
                                (parse-exports
                                  (if (eq? main-exp* 'all)
                                      (map wrap (top-marked-symbols rib))
                                      (append
                                        (map wrap main-exp*)
                                        internal-exp*)))))
                    (seal-rib! rib)
                    (let* ((init* (chi-expr* init* r mr))
                           (rhs* (chi-rhs* rhs* r mr)))
                      (unseal-rib! rib)
                      (let ((loc* (map gen-global lex*))
                            (export-subst (make-export-subst exp-name* exp-id*)))
                        (define errstr
                          "attempt to export mutated variable")
                        (let-values (((export-env global* macro*)
                                      (make-export-env/macros lex* loc* r)))
                          (unless (eq? main-exp* 'all)
                            (for-each
                              (lambda (s) 
                                (let ((name (car s)) (label (cdr s)))
                                  (let ((p (assq label export-env)))
                                    (when p
                                      (let ((b (cdr p)))
                                        (let ((type (car b)))
                                          (when (eq? type 'mutable)
                                            (syntax-violation 'export
                                              errstr name))))))))
                                export-subst))
                          (let ((invoke-body
                                 (build-library-letrec* no-source
                                   name lex* loc* rhs*
                                   (if (null? init*) 
                                       (build-void)
                                       (build-sequence no-source init*))))
                                (invoke-definitions 
                                  (map build-global-define (map cdr global*))))
                            (values
                              (itc) (rtc) (vtc)
                              (build-sequence no-source 
                                (append invoke-definitions
                                  (list invoke-body)))
                              macro* export-subst export-env))))))))))))))

  (define stale-when-collector (make-parameter #f))

  (define (make-stale-collector)
    (let ([code (build-data no-source #f)]
          [req* '()])
      (case-lambda
        [() (values code req*)]
        [(c r*) 
         (set! code 
           (build-conditional no-source 
             code 
             (build-data no-source #t)
             c))
         (set! req* (set-union r* req*))])))

  (define (handle-stale-when guard-expr mr)
    (let ([stc (make-collector)])
      (let ([core-expr (parameterize ([inv-collector stc])
                         (chi-expr guard-expr mr mr))])
        (cond
          [(stale-when-collector) => 
           (lambda (c) (c core-expr (stc)))]))))

  (define core-library-expander
    (case-lambda
      ((e) (core-library-expander e (lambda (x) values)))
      ((e verify-name)
       (let-values (((name* exp* imp* b*) (parse-library e)))
         (let-values (((name ver) (parse-library-name name*)))
           (verify-name name)
           (let ([c (make-stale-collector)])
           (let-values (((imp* invoke-req* visit-req* invoke-code
                               visit-code export-subst export-env)
                           (parameterize ([stale-when-collector c])
                             (library-body-expander name exp* imp* b* #f))))
               (let-values ([(guard-code guard-req*) (c)])
              (values name ver imp* invoke-req* visit-req* 
                      invoke-code visit-code export-subst
                      export-env guard-code guard-req*)))))))))
  
  (define (parse-top-level-program e*)
    (syntax-match e* ()
      (((import imp* ...) b* ...)
       (eq? (syntax->datum import) 'import)
       (values imp* b*))
      (((import . x) . y)
       (eq? (syntax->datum import) 'import)
       (syntax-violation 'expander 
         "invalid syntax of top-level program" (syntax-car e*)))
      (_ 
       (assertion-violation 'expander 
         "top-level program is missing an (import ---) clause"))))


  ;;; An env record encapsulates a substitution and a set of
  ;;; libraries.
  (define-record env (names labels itc)
    (lambda (x p wr)
      (display "#<environment>" p)))

  (define-record interaction-env (rib r locs)
    (lambda (x p wr)
      (display "#<interactive-environment>" p)))
      
  (define (interaction-environment-symbols)
    (environment-symbols (interaction-environment)))
    
  (define (environment-bindings e)
    (vector->list
      (vector-map
        (lambda (name label)
          (parse-binding (cons name (imported-label->binding label))))
        (env-names e)
        (env-labels e))))
            
  (define (parse-binding b)
    (cons (car b) 
      (case (cadr b) 
        ((core-prim global) 'procedure)
        ((core-macro macro global-macro) 'syntax)
        (($core-rtd $rtd) 'record)
        (else 
          (if (eq? (car b) (cadr b)) 
              'syntax 
              (begin
                ;(display (cadr b))
                ;(newline)
                'unknown))))))            
  
  (define environment?
    (lambda (x) (or (env? x) (interaction-env? x))))
  
  (define (environment-symbols x)
    (cond
      ((env? x) (vector->list (env-names x)))
      ((interaction-env? x) 
       (map values (rib-sym* (interaction-env-rib x))))
      (else
       (assertion-violation 'environment-symbols "not an environment" x))))

  ;;; This is R6RS's environment.  It parses the import specs 
  ;;; and constructs an env record that can be used later by 
  ;;; eval and/or expand.
  (define environment
    (lambda imp*
      (let ((itc (make-collector)))
        (parameterize ((imp-collector itc))
          (let-values (((subst-names subst-labels)
                        (parse-import-spec* imp*)))
            (make-env subst-names subst-labels itc))))))
  
  ;;; R6RS's null-environment and scheme-report-environment are
  ;;; constructed simply using the corresponding libraries.
  (define (null-environment n)
    (unless (eqv? n 5)
      (assertion-violation 'null-environment "not 5" n))
    (environment '(psyntax null-environment-5)))
  (define (scheme-report-environment n)
    (unless (eqv? n 5)
      (assertion-violation 'scheme-report-environment "not 5" n))
    (environment '(psyntax scheme-report-environment-5)))

  ;;; The expand procedure is the interface to the internal expression
  ;;; expander (chi-expr).   It takes an expression and an environment.
  ;;; It returns two values: The resulting core-expression and a list of
  ;;; libraries that must be invoked before evaluating the core expr.
  (define core-expand
    (lambda (x env)
      (cond
        ((env? env)
         (let ((rib (make-top-rib (env-names env) (env-labels env))))
           (let ((x (make-stx x top-mark* (list rib) '()))
                 (itc (env-itc env))
                 (rtc (make-collector))
                 (vtc (make-collector)))
               (let ((x
                      (parameterize ((top-level-context #f)
                                     (inv-collector rtc)
                                     (vis-collector vtc)
                                     (imp-collector itc))
                         (chi-expr x '() '()))))
                 (seal-rib! rib)
                 (values x (rtc))))))
        ((interaction-env? env)
         (let ((rib (interaction-env-rib env))
               (r (interaction-env-r env))
               (rtc (make-collector)))
           (let ((x (make-stx x top-mark* (list rib) '())))
             (let-values (((e r^) 
                           (parameterize ((top-level-context env)
                                          (inv-collector rtc)
                                          (vis-collector (make-collector))
                                          (imp-collector (make-collector)))
                             (chi-interaction-expr x rib r))))
               (set-interaction-env-r! env r^)
               (values e (rtc))))))
        (else
         (assertion-violation 'expand "not an environment" env)))))


  ;;; This is R6RS's eval.  It takes an expression and an environment,
  ;;; expands the expression, invokes its invoke-required libraries and
  ;;; evaluates its expanded core form.
  (define eval
    (lambda (x env)
      (unless (environment? env)
        (error 'eval "not an environment" env))
      (let-values (((x invoke-req*) (core-expand x env)))
        (for-each invoke-library invoke-req*)
        (eval-core (expanded->core x)))))
        
  (define expand->core
    (lambda (x env)
      (unless (environment? env)
        (error 'eval "not an environment" env))
      (let-values (((x invoke-req*) (core-expand x env)))
        (for-each invoke-library invoke-req*)
        (expanded->core x))))        


  ;;; Given a (library . _) s-expression, library-expander expands
  ;;; it to core-form, registers it with the library manager, and
  ;;; returns its invoke-code, visit-code, subst and env.

  (define (initial-visit! macro*)
    (for-each (lambda (x)
                 (let ((loc (car x)) (proc (cadr x)))
                   (set-symbol-value! loc proc)))
      macro*))

  (define library-expander
    (case-lambda 
      ((x filename verify-name)
       (define (build-visit-code macro*)
         (if (null? macro*)
             (build-void)
             (build-sequence no-source
               (map (lambda (x)
                      (let ((loc (car x)) (src (cddr x)))
                        (build-global-assignment no-source loc src)))
                    macro*))))
       (let-values (((name ver imp* inv* vis* 
                      invoke-code macro* export-subst export-env
                      guard-code guard-req*)
                     (core-library-expander x verify-name)))
         (let ((id (gensym))
               (name name)
               (ver ver)
               (imp* (map library-spec imp*))
               (vis* (map library-spec vis*))
               (inv* (map library-spec inv*))
               (guard-req* (map library-spec guard-req*))
               (visit-proc (lambda () (initial-visit! macro*)))
               (invoke-proc 
                (lambda () (eval-core (expanded->core invoke-code))))
               (visit-code (build-visit-code macro*))
               (invoke-code invoke-code))
           (install-library id name ver
              imp* vis* inv* export-subst export-env
              visit-proc invoke-proc
              visit-code invoke-code
              guard-code guard-req*
              #t filename)
           (values id name ver imp* vis* inv* 
                   invoke-code visit-code
                   export-subst export-env 
                   guard-code guard-req*))))
      ((x filename)
       (library-expander x filename (lambda (x) (values))))
      ((x)
       (library-expander x #f (lambda (x) (values))))))

  ;;; when bootstrapping the system, visit-code is not (and cannot
  ;;; be) be used in the "next" system.  So, we drop it.
  (define (boot-library-expand x)
    (let-values (((id name ver imp* vis* inv* 
                   invoke-code visit-code export-subst export-env
                   guard-code guard-dep*)
                  (library-expander x)))
      (values name invoke-code export-subst export-env)))
  
  (define (rev-map-append f ls ac)
    (cond
      ((null? ls) ac)
      (else
       (rev-map-append f (cdr ls)
          (cons (f (car ls)) ac)))))
  
  (define build-exports
    (lambda (lex*+loc* init*)
      (build-sequence no-source
        (cons (build-void)
          (rev-map-append
            (lambda (x)
              (build-global-assignment no-source (cdr x) (car x)))
            lex*+loc*
            init*)))))
  
  (define (make-export-subst name* id*)
    (map
      (lambda (name id)
        (let ((label (id->label id)))
          (unless label
            (stx-error id "cannot export unbound identifier"))
          (cons name label)))
      name* id*))
  
  (define (make-export-env/macros lex* loc* r)
    (define (lookup x)
      (let f ((x x) (lex* lex*) (loc* loc*))
        (cond
          ((pair? lex*) 
           (if (eq? x (car lex*)) 
               (car loc*)
               (f x (cdr lex*) (cdr loc*))))
          (else (assertion-violation 'lookup-make-export "BUG")))))
    (let f ((r r) (env '()) (global* '()) (macro* '()))
      (cond
        ((null? r) (values env global* macro*))
        (else
         (let ((x (car r)))
           (let ((label (car x)) (b (cdr x)))
             (case (binding-type b)
               ((lexical)
                (let ((v (binding-value b)))
                  (let ((loc (lookup (lexical-var v)))
                        (type (if (lexical-mutable? v) 
                                  'mutable
                                  'global)))
                    (f (cdr r)
                       (cons (cons* label type loc) env)
                       (cons (cons (lexical-var v) loc) global*)
                       macro*))))
               ((local-macro)
                (let ((loc (gensym)))
                  (f (cdr r)
                     (cons (cons* label 'global-macro loc) env)
                     global*
                     (cons (cons loc (binding-value b)) macro*))))
               ((local-macro!)
                (let ((loc (gensym)))
                  (f (cdr r)
                     (cons (cons* label 'global-macro! loc) env)
                     global*
                     (cons (cons loc (binding-value b)) macro*))))
               ((local-ctv)
                (let ((loc (gensym)))
                  (f (cdr r)
                     (cons (cons* label 'global-ctv loc) env)
                     global*
                     (cons (cons loc (binding-value b)) macro*))))
               (($rtd $module $fluid) 
                (f (cdr r) (cons x env) global* macro*))
               (else
                (assertion-violation 'expander "BUG: do not know how to export"
                       (binding-type b) (binding-value b))))))))))
  
  (define generate-temporaries
    (lambda (ls)
      (syntax-match ls ()
        ((ls ...)
         (map (lambda (x)
                (make-stx 
                  (let ((x (syntax->datum x)))
                    (cond
                      ((or (symbol? x) (string? x)) 
                       (gensym x))
                      (else (gensym 't))))
                  top-mark* '() '()))
              ls))
        (_ 
         (assertion-violation 'generate-temporaries "not a list")))))
  
  (define free-identifier=?
    (lambda (x y)
      (if (id? x)
          (if (id? y)
              (free-id=? x y)
              (assertion-violation 'free-identifier=? "not an identifier" y))
          (assertion-violation 'free-identifier=? "not an identifier" x))))
  
  (define bound-identifier=?
    (lambda (x y)
      (if (id? x)
          (if (id? y)
              (bound-id=? x y)
              (assertion-violation 'bound-identifier=? "not an identifier" y))
          (assertion-violation 'bound-identifier=? "not an identifier" x))))
  
  (define (make-source-condition x)
    (define-condition-type &source-information &condition
      make-source-condition source-condition?
      (file-name source-filename)
      (character source-character))
    (if (pair? x)
        (make-source-condition (car x) (cdr x))
        (condition)))

  (define (extract-position-condition x)
    (make-source-condition (expression-position x)))

  (define (expression-position x)
    (and (stx? x)
         (let ((x (stx-expr x)))
           (and (annotation? x)
                (annotation-source x)))))

  (define (syntax-annotation x)
    (cond 
      [(stx? x) 
        (let ([expr (stx-expr x)]
              [ae* (stx-ae* x)])
          (cond 
            [(annotation? expr) expr]
            [(and  (pair? ae*)); (not expr) (null? (cdr ae*))) 
              (syntax-annotation (car ae*))]
            ;[(annotation? ae*) ae*]
            ;[(stx? ae*)  (syntax-annotation ae*)]
            ;[(pair? expr) (syntax-annotation (car expr))]
            [else x]))]
      ;[(pair? x) (syntax-annotation (car x))]
      [else x]))

;  (define (syntax-annotation x)
;    (if (stx? x)
;        (let ([expr (stx-expr x)])
;          (if (annotation? x)
;              x
;              (stx->datum x)))
;        (stx->datum x)))

  (define (assertion-error expr pos)
    (raise
      (condition
        (make-assertion-violation)
        (make-who-condition 'assert)
        (make-message-condition "assertion failed")
        (make-irritants-condition (list expr))
        (make-source-condition pos))))

  (define syntax-error
    (lambda (x . args)
      (unless (for-all string? args)
        (assertion-violation 'syntax-error "invalid argument" args))
      (raise 
        (condition 
          (make-message-condition
            (if (null? args) 
                "invalid syntax"
                (apply string-append args)))
          (make-syntax-violation 
            (syntax->datum x)
            #f)
          (extract-position-condition x)
          (extract-trace x)))))

  (define (extract-trace x)
    (define-condition-type &trace &condition
      make-trace trace?
      (form trace-form))
    (let f ((x x))
      (cond
        ((stx? x)
         (apply condition 
            (make-trace x)
            (map f (stx-ae* x))))
        ((annotation? x)
         (make-trace (make-stx x '() '() '())))
        (else (condition)))))

    
  (define syntax-violation* 
    (lambda (who msg form condition-object)
       (unless (string? msg) 
         (assertion-violation 'syntax-violation "message is not a string" msg))
       (let ((who
              (cond
                ((or (string? who) (symbol? who)) who)
                ((not who) 
                 (syntax-match form ()
                   (id (id? id) (syntax->datum id))
                   ((id . rest) (id? id) (syntax->datum id))
                   (_  #f)))
                (else
                 (assertion-violation 'syntax-violation 
                    "invalid who argument" who)))))
         (raise 
           (condition
             (if who 
                 (make-who-condition who)
                 (condition))
             (make-message-condition msg)
             condition-object
             (extract-position-condition form)
             (extract-trace form))))))

  (define syntax-violation
    (case-lambda
      ((who msg form) (syntax-violation who msg form #f))
      ((who msg form subform) 
       (syntax-violation* who msg form 
          (make-syntax-violation 
            (syntax->datum form)
            (syntax->datum subform))))))

  (define identifier? (lambda (x) (id? x)))
  
  (define datum->syntax
    (lambda (id datum)
      (if (id? id)
          (datum->stx id datum)
          (assertion-violation 'datum->syntax "not an identifier" id))))
  
  (define syntax->datum
    (lambda (x) (stx->datum x)))

  (define top-level-expander
    (lambda (e*)
      (let-values (((imp* b*) (parse-top-level-program e*)))
        (let-values (((imp* invoke-req* visit-req* invoke-code
                       macro* export-subst export-env)
                      (library-body-expander '() 'all imp* b* #t)))
          (values invoke-req* invoke-code macro* 
                  export-subst export-env)))))

  (define compile-r6rs-top-level
    (lambda (x*)
      (let-values (((lib* invoke-code macro* export-subst export-env) 
                    (top-level-expander x*)))
        (lambda ()
          (for-each invoke-library lib*)
          (initial-visit! macro*)
          (eval-core (expanded->core invoke-code))
          (make-interaction-env 
            (subst->rib export-subst)
            (map 
              (lambda (x)
                (let ([label (car x)] [binding (cdr x)])
                  (let ([type (car binding)] [val (cdr binding)])
                   (cons* label type '*interaction* val))))
              export-env)
            '())))))

  (define (subst->rib subst)
    (let ([rib (make-empty-rib)])
      (set-rib-sym*! rib (map car subst))
      (set-rib-mark**! rib 
        (map (lambda (x) top-mark*) subst))
      (set-rib-label*! rib (map cdr subst))
      rib))  
        
  (define pre-compile-r6rs-top-level
    (lambda (x*)
      (let-values (((lib* invoke-code  macro* export-subst export-env) 
                    (top-level-expander x*)))
        (for-each invoke-library lib*)
        (initial-visit! macro*)
        (compile-core (expanded->core invoke-code)))))

  (define new-interaction-environment
    (case-lambda 
      [() (new-interaction-environment '(ironscheme))]    
      [(libname)
        (let ((lib (find-library-by-name libname))
              (rib (make-empty-rib)))
          (let ((subst (library-subst lib))) 
            (set-rib-sym*! rib (map car subst))
            (set-rib-mark**! rib 
              (map (lambda (x) top-mark*) subst))
            (set-rib-label*! rib (map cdr subst)))
          (make-interaction-env rib '() '()))]))

  (define interaction-environment (make-parameter #f))
  (define top-level-context (make-parameter #f))

  ;;; register the expander with the library manager
  (current-library-expander library-expander))



