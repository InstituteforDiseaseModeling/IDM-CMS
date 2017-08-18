==========================
Gibson-Bruck Next Reaction
==========================

The Gibson-Bruck Next Reaction method [1]_ is an exact stochatic simulation algorithm that is more efficient than the standard Gillespie SSA.  Specifically, this method uses only a single random number per simulation event.  Also, the simulation time is proportional to the logarithm of the number of reactions.  The implementation of the next reaction method in this framework is based on the work by Gibson et. al [1]_.  

**.json file template**

.. literalinclude:: /json_templates/gibsonbruck.json
	:language: JSON

**.json syntax for Gibson-Bruck Next Reaction Method**

``solver``
	takes a string as an input. :code:`Next`, :code:`NextReaction`, and :code:`GobsonBruck` are all valid names to run this solver.

.. rubric:: Footnotes
.. [1] `Gibson, B. and Bruck, J. "Efficient Exact Stochastic Simulation of Chemical Systems with Many Species and Many Channels." The Journal of Physical Chemistry A, 104 9 (2000): 1876-1889. <http://pubs.acs.org/doi/abs/10.1021/jp993732q>`_