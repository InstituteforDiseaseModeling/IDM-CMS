===================
TransportSSA (ISSA)
===================

Inhomogeneous Stochastic Simulation Algorithm or ISSA [1]_, is a solver developed
for simulating diffusion processes in compartmental models. Diffusion events are modeled as
particles that transition to neighboring locales. The ISSA method is ideally suited for systems with
a large number of particles, on the order of thousands of particles or more at each locale.

Diffusion solver errors are all first-order in time and second-order in space, and are therefore
similar to using one of the leaping algorithms for the simulation of diffusion processes.  However,
the diffusion methods execute single reaction events per time step, as opposed to leaping algorithms
that execute multiple reaction events per time step, and are therefore useful if you choose
to capture detailed events.

.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**
   ITSSA**, **TransportSSA**, **DiffusionSSA**, and **TSSA** are all valid names to run this solver."
   epsilon, float, 0.01, "Determines the error of the approximation; accepts values greater than 0 and  much less than 1.  A value of close to 0 is equivalent to a :doc:`gillespie` simulation and a value close to 1 is the most aggressive speedup (and largest error). We do not recommend changing this value."
   greenFunctionIterations, integer, 100, "The number of iterations used to compute the fundamental solution of the diffusion equation; accepts values between 1 and infinity."
   verbose, bool, false, "If true, extra information is printed to the command line, which can  be useful for debugging or testing the solver."

Example
=======

The .cfg file example below is followed by a portion of an an .emodl file to show how diffusive
events are specified. **D** represents the diffusion coefficient and that the reactions specify
transitions of species A to neighboring locales.

.. literalinclude:: /json_templates/TransportSSA.json
	:language: JSON

.. literalinclude:: /emodl_templates/TransportSSA.emodl
	:language: lisp



.. [1] `S. Lampoudi, D.T. Gillespie, & L.R. Petzold, "The multinomial simulation algorithm for discrete stochastic simulation of reaction-diffusion systems". Journal of Chemical Physics 130 9 (2009): 094104. <http://aip.scitation.org/doi/full/10.1063/1.3074302>`_

