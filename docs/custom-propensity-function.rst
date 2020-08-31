==========================
Custom propensity function
==========================

In traditional compartmental modeling, the rate at which a reaction occurs obeys the law of mass
action. In other words, it is directly proportional to the population of the reacting
compartment(s). For example, an EMODL SEIR model with waning immunity will contain reaction terms as
the following:

.. code-block:: lisp


	(reaction exposure   (S I) (E I) (* Ki S I))
	(reaction infection  (E)   (I)   (* Kl E))
	(reaction recovery   (I)   (R)   (* Kr I))
	(reaction waning     (R)   (S)   (* Kw R))


However, mass action dynamics can be too restricting in modeling a complex epidemiological system
with mechanisms such as seasonal forcing, pulse vaccination, and discrete aging. |CMS_s| provides a
set of  :ref:`function_list` to aid formulating custom propensity functions. Below is a part of the
Garki model [1]_ that involves seasonal forcing:

.. code-block:: lisp

	; seasonal parameter
	(func C (* 0.2 (+ 1.01 (sin (* (/ time 365) 2 pi)))))
	; infection rate
	(func h (* g (- 1 (exp (/ (* (- C) Y1) totalpop)))))

	(reaction recoveryY3 (Y3) (X3) (/ (* Y3 h) (- (exp (/ h r2)) 1)))


.. [1] http://garkiproject.nd.edu/static/documents/garkiproject.pdf (Chapter 10, page 263)


