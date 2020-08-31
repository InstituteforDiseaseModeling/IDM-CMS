========================================
Diffusive finite state projection (DFSP)
========================================


Diffusive finite state projection (DFSP) [1]_ is a :term:`solver` developed for simulating diffusion
processes in compartmental models. Diffusion events are modeled as particles that transition to
neighboring locales. DFSP is ideally suited for systems with a small number of particles, on the
order of tens or hundreds of particles per locale.

Diffusion solver errors are all first-order in time and second-order in space, and are therefore
similar to using one of the leaping algorithms for the simulation of diffusion processes.  However,
the diffusion methods execute single reaction events per time step, as opposed to leaping algorithms
that execute multiple reaction events per time step, and are therefore useful if you choose
to capture detailed events.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**DFSP**, **DiffusionFSP**, and **TransportFSP** are all valid names to run this solver."
   epsilon, float, 0.01, "Determines the error of the approximation; accepts values between 0 and 1.  A value of close to 0 is equivalent to a :doc:`gillespie` simulation and a value close to 1 is the most aggressive speedup (and largest error). We do not recommend changing this value."
   verbose, bool, false, "If true, extra information is printed to the command line, which can  be useful for debugging or testing the solver."
   uMax, integer, 120, "The maximum number of particles per subvolume without violating the error condition. Maximum value is 150; if your problem is expected to have more particles, consider using :doc:`transportssa` instead."

Example
=======

The .cfg file example below is followed by a portion of an an .emodl file to show how diffusive
events are specified. **D** represents the diffusion coefficient and that the reactions specify
transitions of species A to neighboring locales.

.. literalinclude:: /json_templates/DFSP.json
	:language: JSON


.. literalinclude:: /emodl_templates/TransportSSA.emodl
	:language: lisp


.. [1] `Brian Drawert, Michael J Lawson, Linda Petzold, Mustafa Khammash, "The diffusive finite state projection algorithm for efficient simulation of the stochastic reaction-diffusion master equation". The Journal of Chemical Physics 132 (2010): 074101 <http://aip.scitation.org/doi/full/10.1063/1.3310809>`_

