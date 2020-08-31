========================
FractionalDiffusion (FD)
========================

FractionalDiffusion (FD) [1]_ is a :term:`solver` developed for simulating heavy-tailed diffusion events in
compartmental models. As opposed to standard diffusion events that model transitions to neighboring
locales, FD allows transitions to any locale irrespective of how far away it is.  The probability of
a particle jumping to a distant locale is small but non-zero.

The method can use several parameters, but the most important for modeling physical processes are
alpha and Dalpha.


.. csv-table::
   :header: Parameter, Data type, Default, Description
   :widths: 1, 1, 1, 3

   solver, string, NA, "**FractionalDiffusion**, **Fractional**, **FD**, **Levy**, and **LevyFlight** are all valid names to run this solver."
   alpha, float, 1, "Determines the value of the fractional derivative; accepts values greater than 0 and less than or equal to 2.  A value close to 0 results in a diffusive process with very large kurtosis.  A value close to 2 results in Gaussian-like diffusion. The default value results in a Cauchy distribution."
   Dalpha, float, 1, "The diffusion coefficient; accepts values between 0 and infinity."
   h, float, 1, "The physical distance between locales; accepts values greater than zero and less than infinity."
   constant, float, 0.25, "The Courant–Friedrichs–Lewy (CFL) condition for parabolic partial differential equations. that is used for meeting the time step criteria; accepts values greater than 0 but much less than 1."
   truncation, integer, number of locales/4, "The maximum number of locales a particle can jump; accepts values greater than 1 and less than the size of the domain divided by 2. To ensure that particles remain far away from the boundary, the default value is the number of locales divided by 4. We do not recommend changing the default value. For details regarding truncation and boundary effects, see [1]_."
   verbose, bool, false, "If true, extra information is printed to the command line, which can be useful for debugging or testing the solver."

Example
=======

Diffusive events are not specified since the solver assumes that all species can diffusive anywhere
in the domain. Therefore, the .emodl files should just include the reaction events.


.. literalinclude:: /json_templates/FractionalDiffusion.json
	:language: JSON



.. [1] `Basil Bayati, "Fractional diffusion-reaction stochastic simulations".  Journal of Chemical Physics 138 10 (2013): 104117. <http://aip.scitation.org/doi/full/10.1063/1.4794696>`_

