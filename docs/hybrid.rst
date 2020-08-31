
==========
Hybrid SSA
==========

An event queue SSA hybrid is an algorithm that combines a :doc:`gillespie` and an :term:`event queue` [1]_ [2]_ .
For the SSA, the rates associated with the chemical reactions are based on a fundamental observation
that the reactions occur at an average rate.  In epidemiology, this assumption may hold well for
certain state transitions (for example, susceptible to infected), but not others (for example,
infected to recovered).

In the latter case, the hybrid algorithm uses the SSA for the transitions that are similar to
chemical reactions, whereas event queues are used for the other types of transitions such as delays.
For example, the event queue could be utilized for a fixed recovery time of individuals from the
infected state. The combination of these two algorithms allow for a greater range of disease models
to be implemented by the compartmental modeling structure.

.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, **Hybrid** is the only valid name to run this solver.
   method, string, RejectionMethod, "**RejectionMethod** and **ExactMethod** are names of two methods offered by the Hybrid SSA. The **RejectionMethod** is based on the algorithm described in [1]_ .  The **ExactMethod** is based on the algorithm described in [2]_ .  Technically, both methods are exact; there are no approximations.  However, the **ExactMethod** algorithm requires fewer random number generations."

Example
=======

.. literalinclude:: /json_templates/hybridSSA.json
	:language: JSON


.. [1] `Barrio, M., Burrage, K., Leier, A. and Tian, T. "Oscillatory Regulation of Hes1: Discrete Stochastic Delay Modelling and Simulation." (2006) PLoS Computational Biology 2 (9): e117. <https://www.doi.org/10.1371/journal.pcbi.0020117>`_
.. [2] `Cai, X. "Exact stochastic simulation of coupled chemical reactions with delays." The Journal of Chemical Physics 126 (2007): 124108. <https://www.ncbi.nlm.nih.gov/pubmed/17411109>`_
