===========
Input files
===========

You will generally use two input files to run |CMS_s| simulations: the :term:`model file` is
required and the :term:`configuration file` is optional.

The model file contains all information that defines the mathematical model itself. For example, it
defines the :term:`species`, the transition rates, incubation time, daily recovery rate, and many
other parameters specific to the disease being modeled. The model file is in :term:`EMODL` format; the
syntax and available parameters are described in :doc:`model-file`.

The configuration file contains information specific to a simulation. For example, the number of
realizations, the duration of each :term:`realization`, and the solvers to use. You may have several
different configuration files that are used with the same model file. If you do not specify a
configuration file, the simulation runs one realization for 100 time steps. The configuration file
is in :term:`JSON (JavaScript Object Notation)` format, though it uses the ".cfg" file extension.
The syntax and available parameters are described in :doc:`configuration-file`.
