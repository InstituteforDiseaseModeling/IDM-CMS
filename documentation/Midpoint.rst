====================
Midpoint Tau-Leaping
====================

The midpoint tau-Leaping algorithm is a modification of tau-leaping [1]_.  Instead of using the current state of the system to evaluate the propensity functions, an estimated midpoint state is constructed.  Then, this midpoint state is used to evaluate the propensity functions from the current time t.  This modification has a direct analogy in the simulation of deterministic systems.  

**.json file template**

.. literalinclude:: /json_templates/midpoint.json
	:language: JSON

**.json syntax for Midpoint Tau-leaping**

``solver``
	takes a string as an input. :code:`TAU` and :code:`TAULEAPING` ARE the only valid names to run this solver.
``epsilon``
	takes a float as an input.  The parameter :code:`epsilon` is used to compute the largest time step tau that is not likely to result in propensity function changes by more than epsilon multiplied by the sum of all the propensities.  For larger values of tau, the step sizes will also be larger.  If the :code:`epsilon` field is missing in the .json file, then the tau-leaping solver will set the value to 0.01.
``nc``
	takes an integer as an input. The parameter :code:`nc` is used as a threshold to separate critical and noncritical reactions.  A critical reaction is defined as a reaction that is at risk for driving the count of a species below zero.  If :code:`nc` is chosen to be extremely large, then all reactions become critical.  This case would reduce to the exact SSA.  Alternatively, if :code:`nc` is chosen to be zero, then there will not be any noncritical reactions.  The case reduces to the original tauleaping algorithm in [1]_.  If the :code:`nc` field is missing in the .json file, then the tauleaping solver will set the value to 2.
``multiple``
	takes an integer as an input.  The parameter :code:`multiple` is used as a threshold to decide on whether to execute a series of SSA steps instead of a tauleap.  If a leap value of tau is chosen (from the noncritical reaction rates) such that it is less than the :code:`multiple` times 1 over the total propensity rate, than the SSA steps are performed.  If the :code:`multiple` field is missing in the .json file, then the tauleaping solver will set the value to 10.
``SSAruns``
	takes an integer as an input. It defines the number of SSA runs that are performed when the proposed leap size from the noncritical reactions is less than :code:`multiple` times 1 over the total propensity rate.  If the :code:`SSAruns` field is missing in the .json file, then the tau-leaping solver will set the value to 100.

.. rubric:: Footnotes
.. [1] `Gillespie, D T. et al. "Approximate accelerated stochastic simulation of chemically reacting systems." The Journal of Chemical Physics 115 4 (2001): 1716. <http://dx.doi.org/10.1063/1.1378322>`_

