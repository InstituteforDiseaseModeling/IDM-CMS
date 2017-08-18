==========
Hybrid SSA
==========

An event queue SSA hybrid is an algorithm that combines a Gillespie Solver and an event queue [1]_.  For the SSA, the rates associated with the chemical reactions are based on a fundamental observation that the reactions occur at an average rate.  In epidemiology, this assumption may hold well for certain state transitions (e.g. Susceptible to Infected), but not others (e.g. Infected to Recovered).  In the latter case, the hybrid algorithm uses the SSA for the transitions that are similar to chemical reactions, whereas event queues are used for the other types of transitions such as delays.  For example, the event queue could be utilized for a fixed recovery time of individuals from the infected state.  The combination of these two algorithms allow for a greater range of disease models to be implemented by the compartmental modeling structure.


**.json file template**

.. literalinclude:: /json_templates/hybridSSA.json
	:language: JSON

**.json syntax for Hybrid SSA**

``solver``
	takes a string as an input. :code:`Hybrid` is the only valid name to run this solver.
``method``
	takes a string as an input. :code:`RejectionMethod` and :code:`ExactMethod` are names of two methods offered by the Hybrid SSA. The default for this field is :code:`RejectionMethod`.

.. rubric:: Footnotes
.. [1] `Cai, X. "Exact stochastic simulation of coupled chemical reactions with delays." The Journal of Chemical Physics 126 (2007): 124108. <https://www.ncbi.nlm.nih.gov/pubmed/17411109>`_