==========================
Configuration file syntax
==========================

While |CMS_s| does not require a configuration file, running a simulation without one uses default
settings that will not produce scientifically useful results. As a stochastic model, you must run
many realizations in a |CMS_s| simulation for statistically significant results. The configuration
file uses JSON syntax and a .cfg file extension.


The table below shows the basic parameters used in a minimal configuration.


.. csv-table::
    :header: Parameter, Data type, Default, Description
    :widths: 1, 1, 1, 3

    duration, integer, 100, The number of time steps to run the realizations for; specified in unitless time relevant to the timescales of the model. The values will correspond to the units specified in the rates used in the model file.
    runs, integer, 1, The number of realizations to run for the simulation.
    samples, integer, 100, The number of samples of the various observables to take over the duration of the simulation.

Besides the parameters listed above, you will likely want to include configuration settings to
control the solver that is used, the output created, and the pseudo-random number generator. By
default, the :doc:`gillespie` solver is used and the file trajectories.csv is created in the output
directory. See the other topics in this section for all available configuration parameters.


Example
=======

The following example specifies the :doc:`gillespie` algorithm, 100 realizations, a duration of 730
time units (two years for a model with rates specified in days), and 250 samples of each realization
spaced evenly over the 730 time unit duration.

.. literalinclude:: /json_templates/minimal.json
    :language: JSON

.. toctree::
   :maxdepth: 4

   solvers
   output
   random-number-library