===============
Gillespie (SSA)
===============


The Gillespie algorithm [1]_ , also known as the Stochastic Simulation Algorithm (SSA), is an exact
:term:`Monte Carlo method` for numerically generating time trajectories of the system state populations
in accordance with the :term:`chemical master equation (CME)`, which is the governing probability
distribution of all possible states in time in homogeneously mixed population. The Gillespie solver
features the Direct Method (DM) implementation, which is the most commonly used. The other variant
of the algorithm, First Reaction Method (FRM), is theoretically equivalent to the DM but differs in
implementation. FRM is implemented in :doc:`gillespiefirstreaction`.

While these methods are exact, both the Gillespie and the :doc:`gillespiefirstreaction` solvers are
computationally expensive as every reaction and its firing time are explicitly computed.

.. csv-table::
   :header: Parameter, Data type, Description
   :widths: 1, 1, 3

   solver, string, "**Gillespie**, **GillespieDirect**, and **SSA** are all valid names to run this solver."

Example
=======

.. literalinclude:: /json_templates/SSA.json
	:language: JSON


.. [1] `Gillespie, Daniel T. "Exact stochastic simulation of coupled chemical reactions" The Journal of Physical Chemistry 81.25 (1977): 2340-361. <http://pubs.acs.org/doi/abs/10.1021/j100540a008?journalCode=jpchax>`_