========
RLeaping
========

RLeaping [1]_ is a solver developed for speeding up the :doc:`standard stochastic simulation algorithm <Gillespie>` [2]_.  In the standard stochastic simulation algorithm, each reaction is simulated individually.  In RLeaping, reactions are grouped together and executed at the same time. While leaping methods are approximate, they result in faster simulations.  

RLeaping can be supplied with four parameters: ``epsilon``, ``theta``, ``sortingInterval``, and ``verbose``. However, if the user does not supply these values, default values will be used.  It is recommended to not change the default values unless there is reason to do so.  If one wishes to speed up the simulation time, the ``epsilon`` parameter should be made larger.  Note however that the accuracy of the method will decrease if epsilon is made larger.  

**.json RLeaping file template**

.. literalinclude:: /json_templates/RLeaping.json
	:language: JSON

**.json syntax for RLeaping**

``solver``
	takes a string as an input. :code:`R` and :code:`RLeaping` are both valid names to run this solver.

``epsilon`` (optional)
	``epsilon``  takes a float greater than zero 0 and much less than 1 and determines the error of the approximation.  A value of close to zero is equivalent to a standard stochastic simulation and a value close to 1 is the most aggressive speedup (and largest error).  A default value of 0.01 is used which can be overridden by the user.  We recommend not changing this value.  

``theta`` (optional)
	``theta`` takes  a float between 0 and 1 controls the timestep selection. If theta = 0, the most conversative timestep will be chosen which will limit the occurrence of a negative species value. The default value is zero which is recommended. 

``sorting invterval`` (optional)
	``sorting interval`` a positive real number between zero and infinity. The method will sort the reaction propensities according to this sorting interval. To disable sorting, set the sorting interval greater than or equal to the simulation time. A default value of 365 is used if not supplied by the user which corresponds to sorting the propensities every year (in terms of simulation time)

``verbose`` (optional)
	``verbose`` takes a true or false flag. If true, extra information is printed to the command line, which can be useful for debugging or testing the solver.   

.. rubric:: Footnotes
.. [1] `Anne Auger, Philippe Chatelain, and Petros Koumoutsakos, "R-leaping: Accelerating the stochastic simulation algorithm by reaction leaps" The Journal of chemical physics 125, 084103 (2006).`
.. [2] `Gillespie, Dan T et al. "Refining the weighted stochastic simulation algorithm." The Journal of chemical physics 130 17 (2009): 174103.`