====================
Midpoint tau-leaping
====================

The midpoint tau-leaping algorithm is a modification of :doc:`tau-leaping` [1]_.  Instead of using
the current state of the system to evaluate the propensity functions, an estimated midpoint state is
constructed.  Then, this midpoint state is used to evaluate the propensity functions from the
current time **t**.  This modification has a direct analogy in the simulation of
:term:`deterministic` systems.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, **Tau** and **TauLeaping** are both valid names to run this solver.
   epsilon, float, 0.01, "Computes the largest time step tau that is not likely to result in propensity function changes by more than **epsilon** multiplied by the sum of all the propensities.  For larger values of tau, the step sizes will also be larger."
   nc, integer, 2, "A threshold to separate critical and noncritical reactions.  A critical reaction is any reaction that is at risk for driving the count of a species below zero; all reactions become critical if **nc** is extremely large, reducing to the exact :doc:`gillespie`.  Alternatively, if **nc** is zero, there will not be any critical reactions, reducing it to :doc:`tau-leaping`."
   multiple, integer, 10, "A threshold to decide on whether to execute a series of SSA steps instead of a tau-leap.  If a leap value of tau is chosen (from the noncritical reaction rates) such that it is less than the **multiple** times 1/(total propensity rate), than the SSA steps are performed."
   SSAruns, integer, 100, "The number of SSA runs that are performed when the proposed leap size from the noncritical reactions is less than **multiple** times 1/(total propensity rate)."

Example
=======

.. literalinclude:: /json_templates/midpoint.json
    :language: JSON

.. [1] `Gillespie, D. T. et al. "Approximate accelerated stochastic simulation of chemically reacting systems." The Journal of Chemical Physics 115 4 (2001): 1716. <http://dx.doi.org/10.1063/1.1378322>`_

