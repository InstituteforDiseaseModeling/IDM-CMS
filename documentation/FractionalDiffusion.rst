===================
FractionalDiffusion
===================

FractionalDiffusion (FD) [1]_ is a solver developed for simulating heavy-tailed diffusion events in compartmental models.   As opposed to standard diffusion events that model transitions to neighboring locales, FD allows transitions to any locale  irrespective of how far away it is.  The probability of a particle jumping to a distant locale is small but non-zero. See [1]_ for more details. 

The method several parameters, but the most important of which for modeling physical processes are alpha and Dalpha, which represent the value of the fractional derivative and the diffusion coefficient, respectively. Alpha takes a floating point value between zero and 2, where a value closer to zero will result in a distribution with heavy tails and a value of 2 will result in a Gaussian distribution with rapidly vanishing tails.  Note that the solver does not need parameter specifications and will automatically use default parameters.      

Diffusive events are not specified since the solver assumes that all species can diffusive anywhere in the domain. The emodl files should just include the reaction events. 

**.json FractionalDiffusion file template**

.. literalinclude:: /json_templates/FractionalDiffusion.json
	:language: JSON

**.json syntax for FractionalDiffusion**

``solver``
	takes a string as an input. :code:`FractionalDiffusion`, :code:`Fractional`, :code:`FD`, :code:`Levy`, and :code:`LevyFlight` are all valid names to run this solver.
``alpha`` (optional)
	``alpha``  takes a float greater than 0 and less than or equal to 2 and determines the value of the fractional derivative.  A value close to zero results in a diffusive process with very large kurtosis.  A value close (or equal) to 2 results in Gaussian-like diffusion.  A default value of 1 is used which results in a Cauchy distribution for the diffusion process.   
``Dalpha`` (optional) 
	``Dalpha`` takes a floating point value between zero and infinity. This is the diffusion coefficient. The default value is 1. 
``h`` (optional) 
	``h`` takes a floating point value greater than zero and less than infinity. This is the physical distance between locales. If not specified the value is set to 1. 
``constant`` (optional)
	``constant`` takes a floating point value greater than zero but much less than one.  This is used for meeting the time step criteria and is the Courant–Friedrichs–Lewy (CFL) condition for parabolic partial differential equations.  A default value of 0.25 is used if the user does not specify a value.    
``truncation`` (optional) 
	``truncation`` takes an integer value greater than one and less than the size of the domain divided by 2. This is the maximum number of locales are particle can jump. To ensure that particles remain far away from the boundary, the default value is the number of locales divided by 4. It is not recommended to change the default value. For details regarding truncation and boundary effects, see [1]_. 
``verbose`` (optional) 
	``verbose`` takes a true or false flag. If true, extra information is printed to the command line, which can be useful for debugging or testing the solver

.. rubric:: Footnotes

.. [1] `Basil Bayati, "Fractional Diffusion-Reaction Stochastic Simulations".  Journal of Chemical Physics, 2013.`

