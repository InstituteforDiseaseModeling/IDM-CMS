=======================================================
Doubly weighted stochastic simulation algorithm (dwSSA)
=======================================================


The doubly weighted stochastic simulation algorithm (dwSSA) [1]_ solver is developed solely for
estimating rare event probabilities and thus should not be used for recording time-course
trajectories.

dwSSA requires a set of biasing parameters to reach the rare event of interest. If a set of biasing
parameters is not provided in the .cfg configuration file, dwSSA will execute multilevel cross-
entropy (CE) method prior to the dwSSA simulation to obtain optimal (minimum CE) biasing parameters.
The solver then employs these biasing parameters in selecting the next reaction and the next time
step, yielding a trajectory weight that is a product of likelihood ratios from importance sampling.
For the successful trajectories that reach the rare event, these weights are used to compute an
unbiased estimator of the rare event probability with a confidence interval [2]_.

.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**dwSSA** is the only valid name to run this solver."
   reExpressionName, string, "If unspecified, the solver searches the :term:`model file` for **reExpression**.", "The name of the function that defines the rare event expression in the model file."
   reValName, string, "If unspecified, the solver searches the :term:`model file` for **ReVal**.", "The name of the parameter that defines the rare event value in the model file."
   gammas, vector of floats, "Multilevel cross entropy (CE) simulations are performed to compute optimal gamma values prior to dwSSA simulations.", "The positive real numbers provided in the vector are used as importance sampling parameters in selecting the next reaction and the next time step, as well as in computing the likelihood ratio of a biased trajectory. The length of the vector is equal to the total number of reactions in the model."
   crossEntropyRuns, integer, "100,000", "The number of trajectories simulated in each level of multilevel CE simulations (not required for dwSSA simulations). Accepts values greater than or equal to 5000. If **crossEntropyRuns** * **crossEntropyThreshold** is less than **crossEntropyMinDataSize**, the value of **crossEntropyRuns** is dynamically adjusted to be the smallest integer greater than **crossEntropyMinDataSize** / **crossEntropyThreshold**."
   crossEntropyThreshold, float, 0.01, "The fraction of top-performing trajectories chosen to compute an intermediate rare event in multilevel CE simulations (not required for dwSSA simulations). Accepts values between 0 and 1.

   .. note::

       If slow convergence is detected during the multilevel CE simulations, the value is decreased to 80% of its previous value.

   "
   crossEntropyMinDataSize, integer, 200, "The minimum number of successful trajectories required to compute an intermediate rare event for multilevel CE simulations. Accepts values greater than or equal to 100."
   outputFileName, string, "File name in the form of <modelname>\_dwSSA\_1e<log>(<runs>), where the base of <log> is 10 and <modelName> is the name of the model file.", "The name of the output file that includes runs, estimates for the rare event probability, 68% uncertainty, and sample variance."

Example
========

.. literalinclude:: /json_templates/dwSSA.json
	:language: JSON



.. [1] `Daigle, Bernie J et al. "Automated estimation of rare event probabilities in biochemical systems." The Journal of Chemical Physics 134 4 (2011): 04410. <http://aip.scitation.org/doi/10.1063/1.3522769>`_
.. [2] `Gillespie, Dan T et al. "Refining the weighted stochastic simulation algorithm." The Journal of Chemical Physics 130 17 (2009): 174103. <http://aip.scitation.org/doi/full/10.1063/1.3116791>`_
