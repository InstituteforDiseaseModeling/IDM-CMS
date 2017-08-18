========
BLeaping
========

BLeaping solver implements explicit tau leaping method with a fixed step size [1]_. Here, the fixed step size is assumed to be small enough such that propensity function values do not change dramatically between time steps. 

Skipping tau selection and thus never reverting back to SSA may speed up the simulation time. However, accuracy is expected to be low, and it is possible to reach negative population. When the latter phenomenon occurs, BLeaping solver sets the corresponding population to zero and displays a warning message recommending the user to reduce the step size. 

**.json file template**

.. literalinclude:: /json_templates/BLeaping.json
	:language: JSON

**.json syntax for BLeaping**

``solver``
	takes a string as an input. :code:`B`, :code:`BLeap`, and :code:`BLeaping` are all valid names to run this solver.

``Tau``
	takes a float as an input, which is the size of the fixed time step used throughout the simulation.  If :code:`tau` is missing in the .json file, a default value of 0.1 is used. 


.. rubric:: Footnotes
.. [1] `Gillespie, Daniel T. "Approximate accelerated stochastic simulation of chemically reacting systems." Journal Of Chemical Physics 115 4 (2001) <http://aip.scitation.org/doi/abs/10.1063/1.1378322>`_
