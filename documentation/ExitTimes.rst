=========
ExitTimes
=========

ExitTimes [1]_ is a solver developed for computing the time it takes for a specified event to occur.  For example, you may want to compute the time it takes for the infectious population to reach zero.  This method will usually be faster than the standard stochastic simulation algorithm. The method attempts to group the propensities into sets, approximate their values, and sample the final time from multiple gamma distributions. See [1]_ for a detailed derivation.     

ExitTimes can be supplied with two parameters: ``epsilon`` and ``verbose``. However, if the user does not supply these values, default values will be used.  It is recommended to not change the default values unless there is reason to do so.  If one wishes to speed up the simulation time, the ``epsilon`` parameter should be made larger.  Note however that the accuracy of the method will decrease if ``epsilon`` is made larger.  

**.json ExitTimes file template**

.. literalinclude:: /json_templates/ExitTimes.json
	:language: JSON

**.emodl ExitTimes file template**

.. literalinclude:: /emodl_templates/ExitTimes.emodl
	:language: lisp

**.json syntax for ExitTimes**

``solver``
	takes a string as an input. :code:`ExitTimes`, :code:`ET`, and :code:`ExitTime` are all valid names to run this solver.
``epsilon`` (optional)
	``epsilon``  takes a float between 0 and 1 and determines the error of the approximation.  A value of zero is equivalent to a standard stochastic simulation and a value of 1 is the most aggressive speedup (and largest error).  A default value of 0.2 is used which can be overridden by the user.  We recommend not changing this value.  
``verbose`` (optional)
	``verbose`` takes a true or false flag. If true, extra information is printed to the command line, which can be useful for debugging or testing the solver.   

.. rubric:: Footnotes
.. [1] `Basil Bayati, "A Method to Calculate the Exit Time in Stochastic Simulations". https://arxiv.org/abs/1512.04468`


