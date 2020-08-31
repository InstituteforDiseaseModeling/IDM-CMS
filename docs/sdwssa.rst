========================================================================
State-dependent doubly weighted stochastic simulation algorithm (sdwSSA)
========================================================================


Similar to the :doc:`dwssa` solver, the state-dependent doubly weighted stochastic simulation
algorithm (sdwSSA) [1]_ is developed exclusively for estimating rare event probabilities and should
not be used for recording time-course trajectories.

sdwSSA employs state-dependent importance sampling using a set of parameters for each reaction in
the model. If the biasing parameters are not provided in a separate JSON file, sdwSSA will
execute multilevel cross-entropy (CE) method prior to the sdwSSA simulation to obtain optimal
(minimum CE) biasing parameters. The overall flow of the algorithm is the same as the :doc:`dwssa`
solver. After sdwSSA simulations finish, an estimate of the rare event with a confidence interval
are returned as output [2]_.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, **sdwSSA** is the only valid name to run this solver.
   reExpressionName, string, "If unspecified, the solver searches the :term:`model file` for **reExpression**.", "The name of the function that defines the rare event expression in the model file."
   reValName, string, "If unspecified, the solver searches the model file for **ReVal**.", "The name of the parameter that defines the rare event value in the model file."
   gammaSize, integer, 15, "The initial length of importance sampling parameters *per reaction*; accepts positive values."
   binCount, integer, 20, "The minimum number of data required to maintain a single bin; bins are otherwise merged until each bin contains at least the value in **binCount**. Used only for multilevel CE simulations; accepts positive values greater than or equal to 10."
   biasingParametersFileName, string, "File name in the form of <modelName>\_biasingParameters.json.", "The name of the JSON file containing importance sampling (IS) parameters."
   crossEntropyRuns, integer, "100,000", "The number of trajectories simulated in each level of multilevel CE simulations (not required for dwSSA simulations). Accepts values greater than or equal to 5000. If **crossEntropyRuns** * **crossEntropyThreshold** is less than **crossEntropyMinDataSize**, the value of **crossEntropyRuns** is dynamically adjusted to be the smallest integer greater than **crossEntropyMinDataSize** / **crossEntropyThreshold**."
   crossEntropyThreshold, float, 0.01, "The fraction of top-performing trajectories chosen to compute an intermediate rare event in multilevel CE simulations (not required for dwSSA simulations). Accepts values between 0 and 1.

   .. note::

       If slow convergence is detected during the multilevel CE simulations, the value is decreased to 80% of its previous value.

   "
   crossEntropyMinDataSize, integer, 200, "The minimum number of successful trajectories required to compute an intermediate rare event for multilevel CE simulations. Accepts values greater than or equal to 100."
   outputFileName, string, "File name in the form of <modelname>\_sdwSSA\_1e<log>(<runs>), where the base of <log> is 10 and <modelName> is the name of the model file.", "The name of the output file that includes runs, estimates for the rare event probability, 68% uncertainty, and sample variance."

Example
=======

.. literalinclude:: /json_templates/sdwSSA.json
	:language: JSON




.. [1] `Roh, Min K. et al. "State-dependent doubly weighted stochastic simulation algorithm for automatic characterization of stochastic biochemical rare events." The Journal of Chemical Physics 135 (2011): 234108. <http://aip.scitation.org/doi/full/10.1063/1.3668100>`_
.. [2] `Gillespie, Dan T et al. "Refining the weighted stochastic simulation algorithm." The Journal of Chemical Physics 130 17 (2009): 174103. <http://aip.scitation.org/doi/full/10.1063/1.3116791>`_

