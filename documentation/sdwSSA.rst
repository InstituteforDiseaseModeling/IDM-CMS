======
sdwSSA
======


Similar to the :doc:`dwSSA` solver, the state-dependent doubly weighted Stochastic Simulation Algorithm (sdwSSA) [1]_ is developed exclusively for estimating rare event probabilities and should not be used for recording time-course trajectories. 

sdwSSA employees state-dependent importance sampling using a set of parameters for each reaction in the model. If they biasing parameters are not provided in the .json configuration file, sdwSSA will execute multilvel cross-entropy (CE) method prior to the sdwSSA simulation to obtain optimal (minimum CE) biasing parameters. The overall flow of the algorithm is the same as the :doc:`sdwSSA` solver. After sdwSSA simulations finish, an estimate of the rare event with a confidence interval are returned as output [2]_.

**.json file template**

.. literalinclude:: /json_templates/sdwSSA.json
	:language: JSON

**.json syntax for sdwSSA**

``solver``
	takes a string as an input. :code:`sdwSSA` is the only valid names to run this solver.
``reExpressionName``
	takes a string as an input, which is the name of the function that defines the rare event expression in .emodl model file.  If :code:`reExpressionName` field is missing in the .json file, then the sdwSSA solver will search .emodl file for :code:`reExpression` as the default name of the function.
``reValName``
	takes a string as an input, which is the name of the parameter that defines the rare event value in .emodl model file. If :code:`reValName` field is missing in the .json file, then the sdwSSA solver will search .emodl file for :code:`reVal` as the default name of the parameter.
``gammaSize``
	takes a positive integer as an input and defines the initial length of importance sampling parameters :emphasis:`per reaction`. 
``binCount``
	takes a positive integer greater or equal to 10 as an input. It specifies the minimum number of data required to maintain a single bin (bins are merged otherwise until each bin contains at least :code:`binCount` data). :code:`binCount` is used only in multilevel cross entropy (CE) simulations and is not required for sdwSSA simulations. If :code:`binCount` is missing in the .json file but required to run multilevel CE simulations, then a default value of 20 is used.  
``biasingParametersFileName``
	takes a string as an input. It is the name of the .json file containing importance sampling (IS) parameters, which are used in the sdwSSA simulations. If :code:`biasingParametersFileName` is missing, default name is created in the form of :emphasis:`modelName`\_biasingParameters.json and multilevel CE simulations are performed to compute optimal IS parameters prior to running sdwSSA simulations. 
``crossEntropyRuns``
	takes an integer greater or equal to 5000 as an input. It defines the number of trajectories simulated in each level of multilevel CE method. This parameter is used only in multilevel CE simulations and is not required for sdwSSA simulations. If :code:`crossEntropyRuns`  is missing in the .json file but required to run multilevel CE simulations, then a default value of 100,000 is used.  
``crossEntropyThreshold`` 
	takes a real value in (0, 1) as an input and defines the fraction of top performing trajectories chosen to compute an intermediate rare event. It is used only in multilevel CE simulations and not required for sdwSSA simulations. If :code:`crossEntropyThreshold` is missing in the .json file but required to run multilevel CE simulations, then a default value of 0.01 (1%) is used. 
	.. note::

		If slow convergence is detected during the multilevel CE simulations, the value of crossEntropyThreshold is decreased to 80% of its previous value. 
``crossEntropyMinDataSize``
	takes an integer greater or equal to 100 as an input and defines the minimum number of successful trajectories required to compute an intermediate rare event. If :code:`crossEntropyMinDataSize` is missing in the .json file, default value of 200 is used. If :code:`crossEntropyRuns  x  crossEntropyThreshold`  is less than :code:`crossEntropyMinDataSize`, the multilevel CE method dynamically adjusts the value of :code:`crossEntropyRuns` such that :code:`crossEntropyRuns` is set to the smallest integer greater than :code:`crossEntropyMinDataSize` divided by :code:`crossEntropyThreshold`.
``outputFileName``
	takes a string as an input. If :code:`outputFileNate` is missing in the .json file, then a default name is used in the form of :emphasis:`modelName`\_sdwSSA\_1e :literal:`log` (:code:`runs`), where the base of :literal:`log` is 10 and *modelName* is the name of the .emodl file. 

	Recorded information includes :code:`runs`, estimate for the rare event probability, 68\% uncertainty, and sample variance. 


.. rubric:: Footnotes
.. [1] `Roh, Min K. et al. "State-dependent doubly weighted stochastic simulation algorithm for automatic characterization of stochastic biochemical rare events." The Journal of Chemical Physics 135 (2011): 234108. <http://aip.scitation.org/doi/full/10.1063/1.3668100>`_
.. [2] `Gillespie, Dan T et al. "Refining the weighted stochastic simulation algorithm." The Journal of Chemical Physics 130 17 (2009): 174103. <http://aip.scitation.org/doi/full/10.1063/1.3116791>`_

