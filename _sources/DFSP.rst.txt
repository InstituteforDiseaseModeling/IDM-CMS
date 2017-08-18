====
DFSP
====


Diffusive Finite State Projection (DFSP) [1]_ is a solver developed for simulating diffusion processes in compartmental models. Diffusion events are modeled as particles that transition to neighboring locales. DFSP is ideally suited for systems with a small number of particles, i.e. on the order of 10s or 100s of particles per locale.  

In terms of the errors of the diffusion solvers, they are all first-order in time and second-order in space, and are therefore similar to using one of the leaping algorithms for the simulation of diffusion processes.  The diffusion methods however execute single reaction events per time-step, as opposed to leaping algorithms that execute multiple reaction events per time-step, and are therefore useful if the modeler chooses to capture detailed events.  Note that all of the diffusion solvers do not need explicit parameter specifications and will automatically use default parameters.        

**.json DFSP file template**

.. literalinclude:: /json_templates/DFSP.json
	:language: JSON

**A portion of an .emodl file is included below to show how diffusive events are specified for** :doc:`TransportSSA`, **DFSP, and** :doc:`OptimalTransportSSA` **.**

.. literalinclude:: /emodl_templates/TransportSSA.emodl
	:language: lisp

Note that ``D`` represents the diffusion coefficient and that the reactions specify transitions of species A to neighboring locales. 

**.json syntax for DFSP**

``solver``
	takes a string as an input. :code:`DFSP`, :code:`DiffusionFSP`, and :code:`TransportFSP` are all valid names to run this solver.
``epsilon`` (optional)
	``epsilon``  takes a float greater than zero 0 and much less than 1 and determines the error of the approximation.  A value of close to zero is equivalent to a standard stochastic simulation and a value close to 1 is the most aggressive speedup (and largest error).  A default value of 0.01 is used which can be overridden by the user.  We recommend not changing this value.   
``verbose`` (optional)
	``verbose`` takes a true or false flag. If true, extra information is printed to the command line, which can  be useful for debugging or testing the solver.   
``uMax`` (optional for DFSP) 
	``uMax`` takes an integer value between 1 and infinity and is the maximum number of particles per subvolume without violating the error condition. The maximum value is 150, the default value is 120. If your problem is expected to have more than this, consider using TransportSSA for the solver.  

.. rubric:: Footnotes

.. [1] Brian Drawert, Michael J Lawson, Linda Petzold, Mustafa Khammash, "The Diffusive Finite State Projection Algorithm for Efficient Simulation of the Stochastic Reaction-Diffusion Master Equation". Journal of Chemical Physics, 2010. 


