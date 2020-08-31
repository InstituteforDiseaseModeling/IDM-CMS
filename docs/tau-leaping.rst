===========
Tau-leaping
===========


Tau-leaping [1]_ was developed by Gillespie to increase the computational speed of the SSA, which is
an exact method.  Instead of computing the time to every reaction, this algorithm approximates the
process and attempts to leap in time, executing a large number of reactions in a period *tau*.  This
algorithm is computationally faster; however, the approximation removes the "exact" connection to
the solution of the (`master equation <https://en.wikipedia.org/wiki/Master_equation>`_-based
methods) for the system.

The implementation of tau-leaping in |CMS_s| is based on a work by Cao et. al [2]_.  This modified
tau-leaping algorithm helps avoid the possibility of creating negative species counts within a
compartment.

.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, **Tau** and **TauLeaping** are both valid names to run this solver.
   epsilon, float, 0.001, "Computes the largest time step tau that is not likely to result in propensity function changes by more than **epsilon** multiplied by the sum of all the propensities.  For larger values of tau, the step sizes will also be larger."
   Nc, integer, 2, "A threshold to separate critical and noncritical reactions. A critical reaction is any reaction that is at risk for driving the count of a species below zero; all reactions become critical if **nc** is extremely large, reducing to the exact :doc:`gillespie`. Alternatively, if **nc** is zero, there will not be any critical reactions, reducing it to :doc:`tau-leaping`."
   Multiple, integer, 10, "A threshold that determines whether to execute a series of SSA steps instead of a tau-leap.  If a leap value of tau is chosen (from the noncritical reaction rates) such that it is less than the **multiple** times 1/(total propensity rate), than the SSA steps are performed."
   SSARuns, integer, 100, "The number of SSA runs that are performed when the proposed leap size from the noncritical reactions is less than **multiple** times 1/(total propensity rate)."

Example
=======

.. literalinclude:: /json_templates/tauLeaping.json
	:language: JSON


.. [1] `Gillespie, D T. et al. "Approximate accelerated stochastic simulation of chemically reacting systems." The Journal of Chemical Physics 115 4 (2001): 1716. <http://dx.doi.org/10.1063/1.1378322>`_
.. [2] `Cao, Y. et al. "Avoiding negative populations in explicit Poisson tau-leaping." The Journal of Chemical Physics 123 (2005): 054104. <http://dx.doi.org/10.1063/1.1992473>`_
