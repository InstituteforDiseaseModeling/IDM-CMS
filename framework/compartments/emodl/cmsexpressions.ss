(library (emodl cmsexpressions)
    (export
        parse-expression
        parse-boolean
        testunary testbinary testnary testconst testcee testaitch testempty testone
;        testrand
;        test-equal test-not test-lessthan test-lessequal1 test-lessequal2
;        test-greaterthan test-greaterequal1 test-greaterequal2 test-notequal
;        test-and test-and-many test-or test-or-many test-xor
    )
    (import (rnrs) (ironscheme) (ironscheme clr) (ironscheme conversions))

(clr-reference compartments)
(clr-using compartments.emod)
(clr-using compartments.emod.expressions)
(clr-using compartments.emod.interfaces)
(clr-using compartments.emod.utils)
(clr-using compartments.emod.distributions)

(define (new-symbol-reference symbol)
    (clr-new SymbolReference (symbol->string symbol)))

(define (new-constant expression)
    (clr-new Constant (->single (real->flonum expression))))

(define (parse-expression expression)
    (cond
        ((number? expression) (new-constant expression))
        ((symbol? expression) (new-symbol-reference expression))
        ((list? expression) (let ((count (length expression)))
                                (cond
                                    ((= count 1) (parse-func   expression))
                                    ((= count 2) (parse-unary  expression))
                                    ((= count 3) (parse-binary expression))
                                    ((> count 3) (parse-nary   expression))
                                    (#t (error parse-expression "Bad expression: " expression)))))
        (#t (error parse-expression "Invalid expression: " expression))))

(define-syntax new-func
    (syntax-rules ()
        [(_ type args ...)
            (clr-new type args ...)]
    )
)

(define (parse-func expression)
    (case (car expression)
        ((rand)  (new-func RandomNumber))
        (else (error parse-func "Invalid function: " (car expression)))
    )
)

(define-syntax new-unary
    (syntax-rules ()
        [(_ type args ...)
            (clr-new type args ...)]))

(define (parse-unary expression)
    (case (car expression)
        ((-)     (new-unary NegateOperator         (parse-expression (cadr expression))))
        ((exp)   (new-unary ExponentiationOperator (parse-expression (cadr expression))))
        ((ln)    (new-unary LogarithmOperator      (parse-expression (cadr expression))))
        ((sin)   (new-unary SineOperator           (parse-expression (cadr expression))))
        ((cos)   (new-unary CosineOperator         (parse-expression (cadr expression))))
;        ((tan)   (new-unary TangentOperator        (parse-expression (cadr expression))))
;        ((asin)  (new-unary ArcSineOperator        (parse-expression (cadr expression))))
;        ((acos)  (new-unary ArcCosineOperator      (parse-expression (cadr expression))))
;        ((atan)  (new-unary ArcTangentOperator     (parse-expression (cadr expression))))
        ((abs)   (new-unary AbsoluteOperator       (parse-expression (cadr expression))))
        ((floor) (new-unary FloorOperator          (parse-expression (cadr expression))))
        ((ceil)  (new-unary CeilingOperator        (parse-expression (cadr expression))))
        ((sqrt)  (new-unary SqrtOperator           (parse-expression (cadr expression))))
        ((step)  (new-unary HeavisideStepOperator  (parse-expression (cadr expression))))
        ((empirical) (clr-static-call Empirical FromFile (cadr expression)))
        (else (error parse-unary "Invalid operation: " (car expression))))
    )

(define-syntax new-binary
    (syntax-rules ()
        [(_ type args ...)
            (clr-new type args ...)]))
    
(define (parse-binary expression)
    (case (car expression)
        ((+)   (new-binary AddOperator      (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((-)   (new-binary SubtractOperator (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((*)   (new-binary MultiplyOperator (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((/)   (new-binary DivideOperator   (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((%)   (new-binary ModuloOperator   (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((^)   (new-binary PowerOperator    (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((pow) (new-binary PowerOperator    (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((min) (new-binary MinimumOperator  (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((max) (new-binary MaximumOperator  (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((uniform)  (clr-new Uniform (->single (cadr expression)) (->single (caddr expression))))
        ((normal)   (clr-new Normal  (->single (cadr expression)) (->single (caddr expression))))
        ((gaussian) (clr-new Normal  (->single (cadr expression)) (->single (caddr expression))))
        (else (error parse-binary "Invalid operation: " (car expression))))
    )

(define (parse-nary expression)
    (case (car expression)
        ((+)  (new-binary AddOperator      (parse-expression (cadr expression)) (parse-expression (cons '+ (cddr expression)))))
        ((sum)(new-binary AddOperator      (parse-expression (cadr expression)) (parse-expression (cons '+ (cddr expression)))))
        ((*)  (new-binary MultiplyOperator (parse-expression (cadr expression)) (parse-expression (cons '* (cddr expression)))))
        (else (error parse-nary "Invalid operation: " (car expression))))
    )

(define (testunary)  (parse-expression '(sin (/ pi 2))))
(define (testbinary) (parse-expression '(^ 2 10)))
(define (testnary)   (parse-expression '(+ X1 X2 X3 X4 Y1 Y2 Y3)))
(define (testconst)  (parse-expression 3.14159265))
(define (testcee)    (parse-expression '(* 0.2 (+ 1 (sin (* (/ time 365) 2 pi))))))
(define (testaitch)  (parse-expression '(- 1.0 (exp (/ (* (- C) Y1) (+ X1 X2 X3 X4 Y1 Y2 Y3))))))
(define (testempty)  (parse-expression '()))
(define (testone)    (parse-expression '(1)))
(define (testrand)   (parse-expression '(rand)))

(define (parse-boolean expression)
    (cond
;        ((symbol? expression) (new-symbol-reference expression))
        ((list?   expression) (let ((count (length expression)))
            (cond
                ((= count 2) (parse-boolean-unary  expression))
                ((= count 3) (parse-boolean-binary expression))
                ((> count 3) (parse-boolean-nary   expression))
                (#t (error parse-boolean "Bad boolean expression: " expression))
            )))
        (#t (error parse-boolean "Invalid boolean expression: " expression))
    )
)

(define-syntax new-boolean-unary
    (syntax-rules ()
        [(_ type args ...) (clr-new type args ...)]
    )
)

(define (parse-boolean-unary expression)
    (case (car expression)
        ((!) (new-boolean-unary NotOperator (parse-boolean (car (cdr expression)))))
        (else (error parse-boolean-unary "Invalid operation: " (car expression)))
    )
)

(define-syntax new-boolean-binary
    (syntax-rules ()
        [(_ type args ...) (clr-new type args ...)]
    )
)

(define (parse-boolean-binary expression)
    (case (car expression)
        ((&)   (new-boolean-binary AndOperator (parse-boolean (cadr expression)) (parse-boolean (caddr expression))))
        ((and) (new-boolean-binary AndOperator (parse-boolean (cadr expression)) (parse-boolean (caddr expression))))
        ((or)  (new-boolean-binary OrOperator  (parse-boolean (cadr expression)) (parse-boolean (caddr expression))))
        ((^)   (new-boolean-binary XorOperator (parse-boolean (cadr expression)) (parse-boolean (caddr expression))))
        ((<)   (new-boolean-binary LessThanOperator            (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((<=)  (new-boolean-binary LessThanOrEqualOperator     (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((>)   (new-boolean-binary GreaterThanOperator         (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((>=)  (new-boolean-binary GreaterThanOrEqualOperator  (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((==)  (new-boolean-binary EqualToOperator             (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        ((!=)  (new-boolean-binary NotEqualToOperator          (parse-expression (cadr expression)) (parse-expression (caddr expression))))
        (else  (error parse-boolean-binary "Invalid operation: " (car expression)))
    )
)

(define (parse-boolean-nary expression)
    (case (car expression)
        ((&)  (new-boolean-binary AndOperator (parse-boolean (cadr expression)) (parse-boolean (cons '&  (cddr expression)))))
        ((or) (new-boolean-binary OrOperator  (parse-boolean (cadr expression)) (parse-boolean (cons 'or (cddr expression)))))
        (else (error parse-boolean-nary "Invalid operation: " (car expression)))
    )
)

;(define test-equal         (parse-boolean '(== 3 3)))
;(define test-not           (parse-boolean '(! (== 3 4))))
;(define test-lessthan      (parse-boolean '(< 3 4)))
;(define test-lessequal1    (parse-boolean '(<= 3 4)))
;(define test-lessequal2    (parse-boolean '(<= 4 4)))
;(define test-greaterthan   (parse-boolean '(> 4 3)))
;(define test-greaterequal1 (parse-boolean '(>= 4 3)))
;(define test-greaterequal2 (parse-boolean '(>= 4 3)))
;(define test-notequal      (parse-boolean '(!= 6 5)))
;(define test-and           (parse-boolean '(& (> 4 3) (< 2 3))))
;(define test-and-many      (parse-boolean '(& (> 9 8) (> 7 6) (> 5 4) (> 3 2) (> 1 0))))
;(define test-or            (parse-boolean '(or (> 3 4) (> 6 5))))
;(define test-or-many       (parse-boolean '(or (> 0 1) (> 2 3) (> 4 5) (> 6 7) (> 9 8))))
;(define test-xor           (parse-boolean '(^ (> 3 4) (> 6 5))))

); end library
