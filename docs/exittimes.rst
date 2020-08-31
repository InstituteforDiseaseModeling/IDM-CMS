=========
ExitTimes
=========

ExitTimes [1]_ is a :term:`solver` developed for computing the time it takes for a specified event to occur.
For example, you may want to compute the time it takes for the infectious population to reach zero.
This method will usually be faster than :doc:`gillespie`. The method attempts to group the
propensities into sets, approximate their values, and sample the final time from multiple gamma
distributions. See [1]_ for a detailed derivation.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**ExitTimes**, **ET**, and **ExitTime** are all valid names to run this solver."
   epsilon, float, 0.2, "Determines the error of the approximation; accepts values between 0 and 1.  A value of close to 0 is equivalent to a :doc:`gillespie` simulation and a value close to 1 is the most aggressive speedup (and largest error). We do not recommend changing this value."
   verbose, bool, false, "If true, extra information is printed to the command line, which can be useful for debugging or testing the solver."


Example
=======

The .cfg file example below is followed by a portion of an an .emodl file to show how exit time
events are specified.

.. literalinclude:: /json_templates/ExitTimes.json
	:language: JSON


.. literalinclude:: /emodl_templates/ExitTimes.emodl
	:language: lisp


.. [1] `Basil Bayati, "A Method to Calculate the Exit Time in Stochastic Simulations" <https://arxiv.org/abs/1512.04468>`_
