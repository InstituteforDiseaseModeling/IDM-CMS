===========
Tau-Leaping
===========


Tau-Leaping [1]_ is developed by Gillespie to increase the computational speed of the SSA, whicih is an exact method.  Instead of computing the time to every reaction, this algorithm approximates the process and attempts to leap in time, executing a large number of reactions in a period `tau`.  This algorithm is computationally faster; however, the approximation removes the “exact” connection to the solution of the (`master equation <https://en.wikipedia.org/wiki/Master_equation>`_-based methods) for the system.

The implementation of tau-leaping in CMS is based on a work by Cao et. al [2]_.  This modified tau-leaping algorithm helps avoid the possibility of creating negative species counts within a compartment.

**.json file template**

.. literalinclude:: /json_templates/tauLeaping.json
	:language: JSON

**.json syntax for Tau-leaping**

``solver``
	takes a string as an input. :code:`Tau` and :code:`TauLeaping` are both valid names to run this solver.
``epsilon``
	takes a float as an input.  The parameter :code:`epsilon` is used to compute the largest time step tau that is not likely to result in propensity function changes by more than epsilon multiplied by the sum of all the propensities.  For larger values of tau, the step sizes will also be larger.  If the :code:`epsilon` field is missing in the .json file, then the tau-leaping solver will set the value to 0.001.
``nc``
	takes an integer as an input. The parameter :code:`nc` is used as a threshold to separate critical and noncritical reactions.  A critical reaction is defined as a reaction that is at risk for driving the count of a species below zero.  If :code:`nc` is chosen to be extremely large, then all reactions become critical.  This case would reduce to the exact :doc:`SSA <Gillespie>`.  Alternatively, if :code:`nc` is chosen to be zero, then there will not be any noncritical reactions.  The case reduces to the :doc:`original tauleaping algorithm <TauLeaping>` in [1]_.  If the :code:`nc` field is missing in the .json file, then a default value of 2 is used. 
``multiple``
	takes an integer as an input.  The parameter :code:`multiple` is used as a threshold to decide on whether to execute a series of SSA steps instead of a tauleap.  If a leap value of tau is chosen (from the noncritical reaction rates) such that it is less than the :code:`multiple` times 1 over the total propensity rate, than the SSA steps are performed.  If the :code:`multiple` field is missing in the .json file, then a default value of 10 is used. 
``SSAruns``
	takes an integer as an input. It defines the number of SSA runs that are performed when the proposed leap size from the noncritical reactions is less than :code:`multiple` times 1 over the total propensity rate.  If the :code:`SSAruns` field is missing in the .json file, then a default value of 100 is used. 

.. rubric:: Footnotes
.. [1] `Gillespie, D T. et al. "Approximate accelerated stochastic simulation of chemically reacting systems." The Journal of Chemical Physics 115 4 (2001): 1716. <http://dx.doi.org/10.1063/1.1378322>`_
.. [2] `Cao, Y. et al. "Avoiding negative populations in explicit Poisson tau-leaping." The Journal of Chemical Physics 123 (2005): 054104. <http://dx.doi.org/10.1063/1.1992473>`_
