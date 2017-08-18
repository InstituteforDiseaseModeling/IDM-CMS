===================
OptimalTransportSSA
===================

 
The OptimalTransport Diffusion (OTSSA) is a solver that attempts to automatically choose between the :doc:`TransportSSA` (ISSA) [1]_ and :doc:`Diffusive Finite State Projection (DFSP) <DFSP>` [2]_ solvers. Diffusion events are modeled as particles that transition to neighboring locales. The ISSA method is ideally suited for systems with a large number of particles, i.e. 1000 particles or more at each locale. On the other hand, DFSP is ideally suited for systems with a small number of particles, i.e. on the order of 10s or 100s of particles.  OTSSA will attempt to dynamically choose which of the above solvers to use per time-step.  This solver is recommended if the modeler does not know whether the system contains a small or large number of particles, or if the modeler knows that the system evolve with both large and small populations.

In terms of the errors of the diffusion solvers, they are all first-order in time and second-order in space, and are therefore similar to using one of the leaping algorithms for the simulation of diffusion processes.  The diffusion methods however execute single reaction events per time-step, as opposed to leaping algorithms that execute multiple reaction events per time-step, and are therefore useful if the modeler chooses to capture detailed events.  Note that all of the diffusion solvers do not need explicit parameter specifications and will automatically use default parameters.        

**.json OTSSA file template**

.. literalinclude:: /json_templates/OptimalTransportSSA.json
	:language: JSON

**A portion of an .emodl file is included below to show how diffusive events are specified for** :doc:`TransportSSA` **,** :doc:`DFSP` **, and OTSSA.**

.. literalinclude:: /emodl_templates/TransportSSA.emodl
	:language: lisp

Note that ``D`` represents the diffusion coefficient and that the reactions specify transitions of species A to neighboring locales. 

**.json syntax for OTSSA**

``solver``
	takes a string as an input. :code:`OTSSA`, :code:`OptimalTransportSSA`, and :code:`DFSPPrime` are all valid names to run this solver.
``epsilon`` (optional)
	``epsilon``  takes a float greater than zero 0 and much less than 1 and determines the error of the approximation.  A value of close to zero is equivalent to a standard stochastic simulation and a value close to 1 is the most aggressive speedup (and largest error).  A default value of 0.01 is used which can be overridden by the user.  We recommend not changing this value.   
``verbose`` (optional)
	``verbose`` takes a true or false flag. If true, extra information is printed to the command line, which can  be useful for debugging or testing the solver.   
``greensFunctionIterations`` (optional for ISSA)  
	``greensFunctionIterations`` takes an integer value between 1 and infinity and is the number of iterations used to compute the fundamental solution of the diffusion equation. The default value is 100. 
``uMax`` (optional for DFSP) 
	``uMax`` takes an integer value between 1 and infinity and is the maximum number of particles per subvolume without violating the error condition. The maximum value is 150, the default value is 120. If your problem is expected to have more than this, consider using TransportSSA for the solver.  
``transportSSAThreshold`` (optional for OTSSA) 
	``transportSSAThreshold`` takes an integer value between 1 and infinity and is the threshold for choosing when to run TransportSSA vs DFSP. The default value is uMax * graphDimension, where graphDimension is the dimension of the diffusion problem (this is calculated internally using the number of neighbors). It is not advisable to override the default value of this parameter.    


.. rubric:: Footnotes
.. [1] S. Lampoudi, D.T. Gillespie, & L.R. Petzold, "The Multinomial Simulation Algorithm for Discrete Stochastic Simulation of Reaction-Diffusion Systems". Journal of Chemical Physics, 2009. 
.. [2] Brian Drawert, Michael J Lawson, Linda Petzold, Mustafa Khammash, "The Diffusive Finite State Projection Algorithm for Efficient Simulation of the Stochastic Reaction-Diffusion Master Equation". Journal of Chemical Physics, 2010. 


