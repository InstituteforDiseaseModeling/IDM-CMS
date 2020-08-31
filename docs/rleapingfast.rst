============
RLeapingFast
============

RLeapingFast [1]_ [2]_ is a solver developed for speeding up the :doc:`gillespie` [3]_.  In the
standard SSA, each reaction is simulated individually. The difference between RLeaping [1]_ and
RLeapingFast [2]_ is the computation of how many reactions to leap over, which is likely to be
quicker with RLeapingFast [2]_. This method uses the time step computation developed for tau-leaping
[2]_ and recasts it for use in RLeapingFast. While leaping methods are approximate, they result in
faster simulations.

RLeaping can be supplied with four parameters, but we recommend that you do not change the default
values unless there is reason to do so. You can speed up the simulation time by increasing
**epsilon**, but the accuracy of the method will decrease.

.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**RF**, **RFast**, and **RLeaping** are all valid names to run this solver."
   epsilon, float, 0.01, "Determines the error of the approximation; accepts values greater than 0 and much less than 1.  A value of close to 0 is equivalent to a :doc:`gillespie` simulation and a value close to 1 is the most aggressive speedup (and largest error). We do not recommend changing this value."
   theta, float, 0, "Controls the time step selection; accepts values between 0 and 1. A value of 0 is the most conservative and will limit the occurrence of a negative species value."
   sorting interval, float, 365, "Sorts the reaction propensities according to this time interval; accepts positive values. To disable sorting, set the sorting interval greater than or equal to the simulation time."
   verbose, bool, false, "If true, extra information is printed to the command line, which can be useful for debugging or testing the solver."

Example
=======

.. literalinclude:: /json_templates/RLeapingFast.json
	:language: JSON




.. [1] `Anne Auger, Philippe Chatelain, and Petros Koumoutsakos, "R-leaping: Accelerating the stochastic simulation algorithm by reaction leaps" The Journal of Chemical Physics 125 8 (2006): 084103. <http://aip.scitation.org/doi/full/10.1063/1.2218339>`_
.. [2] `Yang Cao, Daniel T. Gillespie, and Linda R. Petzold "Efficient step size selection for the tau-leaping simulation method" Journal of Chemical Physics 124 4 (2006): 044109 <http://aip.scitation.org/doi/full/10.1063/1.2159468>`_
.. [3] `Gillespie, Daniel T. "Exact stochastic simulation of coupled chemical reactions" The Journal of Physical Chemistry 81.25 (1977): 2340-361. <http://pubs.acs.org/doi/abs/10.1021/j100540a008?journalCode=jpchax>`_
