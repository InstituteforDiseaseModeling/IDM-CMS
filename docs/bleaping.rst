========
BLeaping
========

The BLeaping solver implements an explicit tau leaping method with a fixed step size [1]_. Here, the
fixed step size is assumed to be small enough such that propensity function values do not change
dramatically between time steps.

Skipping tau selection and thus never reverting back to :doc:`gillespie` may speed up the simulation
time. However, accuracy is expected to be low, and it is possible to reach negative population. When
the latter phenomenon occurs, the BLeaping solver sets the corresponding population to zero and
displays a warning message recommending that you reduce the time step size.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**B**, **BLeap**, and **BLeaping** are all valid names to run this solver."
   Tau, float, 0.1, The size of the fixed time step used throughout the simulation.

Example
=======

.. literalinclude:: /json_templates/BLeaping.json
	:language: JSON


.. [1] `Gillespie, Daniel T. "Approximate accelerated stochastic simulation of chemically reacting systems." Journal Of Chemical Physics 115 4 (2001) <http://aip.scitation.org/doi/abs/10.1063/1.1378322>`_
