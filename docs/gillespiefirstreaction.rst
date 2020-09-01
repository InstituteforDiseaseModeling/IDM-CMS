======================
GillespieFirstReaction
======================

The Gillespie First Reaction solver implements the First Reaction Method (FRM) version of the Gillespie
algorithm [1]_. The other version, the Direct Method (DM), is implemented in :doc:`gillespie`. While
the two methods are theoretically equivalent, they differ in implementation. FRM generates a
potential reaction time for every reaction and chooses the reaction corresponding to the earliest
reaction time, thus *first reaction*. In DM implementation, the next reaction time is generated
first. Then a reaction is chosen after considering the likelihood of each reaction firing at the
computed time.


.. csv-table::
   :header: Parameter, Data type, Description
   :widths: 1, 1, 3

   solver, string, "**First**, **FirstReaction**, and **GillespieFirstReaction** are all valid names to run this solver."

Example
=======

.. literalinclude:: /json_templates/SSA_first.json
	:language: JSON



.. [1] `Gillespie, Daniel T. "Exact stochastic simulation of coupled chemical reactions." The Journal of Physical Chemistry 81.25 (1977): 2340-361. <http://pubs.acs.org/doi/abs/10.1021/j100540a008?journalCode=jpchax>`_