=====
dwSSA
=====


The doubly weighted Stochastic Simulation Algorithm (dwSSA) [1]_ solver is developed solely for estimating rare event probabilities and thus should not be used for recording time-course trajectories.

dwSSA requires a set of biasing parameters to reach the rare event of interest. If a set of biasing parameters is not provided in the .json configuration file, dwSSA will execute multilevel cross-entropy (CE) method prior to the dwSSA simulation to obtain optimal (minimum CE) biasing parameters. The solver then employs these biasing parameters in selecting the next reaction and the next time step, yielding a trajectory weight that is a product of likelihood ratios from importance sampling. For the successful trajectories that reach the rare event, these weights are used to compute an unbiased estimator of the rare event probability with a confidence interval [2]_.

**.json file template**

.. literalinclude:: /json_templates/dwSSA.json
	:language: JSON

**.json syntax for dwSSA**

``solver``
	takes a string as an input. :code:`dwSSA` is the only valid names to run this solver.
``reExpressionName``
	takes a string as an input, which is the name of the function that defines the rare event expression in .emodl model file.  If :code:`reExpressionName` field is missing in the .json file, then the dwSSA solver will search .emodl file for :code:`reExpression` as the default name of the function.
``reValName``
	takes a string as an input, which is the name of the parameter that defines the rare event value in .emodl model file. If :code:`reValName` field is missing in the .json file, then the dwSSA solver will search .emodl file for :code:`reVal` as the default name of the parameter.
``gammas``
	takes a vector of positive real numbers as an input. The length of :code:`gammas` is equal to the total number of reactions in the model. They are used as importance sampling parameters in selecting the next reaction and the next time step, as well as in computing the likelihood ratio of a biased trajectory. If :code:`gammas` is missing in the .json file, multilevel cross entropy (CE) simulations are performed to compute optimal gamma values prior to dwSSA simulations. 
``crossEntropyRuns``
	takes an integer greater or equal to 5000 as an input. It defines the number of trajectories simulated in each level of multilevel CE method. This parameter is used only in multilevel CE simulations and is not required for dwSSA simulations. If :code:`crossEntropyRuns` is missing in the .json file but required to run multilevel CE simulations, then a default value of 100,000 is used.  
``crossEntropyThreshold`` 
	takes a real value in (0, 1) as an input and defines the fraction of top performing trajectories chosen to compute an intermediate rare event. It is used only in multilevel CE simulations and not required for dwSSA simulations. If :code:`crossEntropyThreshold` is missing in the .json file but required to run multilevel CE simulations, then a default value of 0.01 (1%) is used. 
	.. note::

		If slow convergence is detected during the multilevel CE simulations, the value of crossEntropyThreshold is decreased to 80% of its previous value. 
``crossEntropyMinDataSize``
	takes an integer greater or equal to 100 as an input and defines the minimum number of successful trajectories required to compute an intermediate rare event. If :code:`crossEntropyMinDataSize` is missing in the .json file, default value of 200 is used. If :code:`crossEntropyRuns  x  crossEntropyThreshold`  is less than :code:`crossEntropyMinDataSize`, the multilevel CE method dynamically adjusts the value of :code:`crossEntropyRuns` such that :code:`crossEntropyRuns` is set to the smallest integer greater than :code:`crossEntropyMinDataSize` divided by :code:`crossEntropyThreshold`.
``outputFileName``
	takes a string as an input. If :code:`outputFileNate` is missing in the .json file, then a default name is used in the form of :emphasis:`modelName`\_dwSSA\_1e :literal:`log` (:code:`runs`), where the base of :literal:`log` is 10 and *modelName* is the name of the .emodl file. 

	Recorded information includes :code:`runs`, estimate for the rare event probability, 68\% uncertainty, and sample variance. 

.. rubric:: Footnotes
.. [1] `Daigle, Bernie J et al. "Automated estimation of rare event probabilities in biochemical systems." The Journal of Chemical Physics 134 4 (2011): 04410. <http://aip.scitation.org/doi/10.1063/1.3522769>`_
.. [2] `Gillespie, Dan T et al. "Refining the weighted stochastic simulation algorithm." The Journal of Chemical Physics 130 17 (2009): 174103. <http://aip.scitation.org/doi/full/10.1063/1.3116791>`_
