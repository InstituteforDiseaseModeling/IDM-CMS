=======================================
Introduction to compartmental modeling
=======================================

Compartmental modeling is widely used among epidemiologists to simulate disease dynamics. These
models treat each disease state as a different compartment that contains a homogeneous population of
individuals. Depending on the disease being simulated, the compartments can be susceptible (S),
exposed (E), infectious (I), or recovered (R).

The most common compartmental models are :term:`deterministic`; that is, given the same inputs, they
will always produce the same outputs. Deterministic compartmental models use either an
:term:`ordinary differential equation (ODE)` or a :term:`partial differential equation (PDE)`.
While this type of compartmental model is very fast to simulate, they aren't suited to all
situations.

There are many cases where :term:`stochastic` modeling that uses :term:`chemical master equation
(CME)` based methods is preferred. See `CME on Wikipedia
<https://en.wikipedia.org/wiki/Master_equation>`_ for more information. Stochastic framework
provides distributions associated with characteristics of a process and rigorous procedures for
inference. In addition, deterministic models do not provide an accurate description of the system
when the population in any of the compartments is low.

For example, consider the following simple SIR model.


.. math::

    \begin{aligned}
    S + I & \stackrel{\beta}{\rightarrow} 2I,&   \beta = 0.001 \\
    I & \stackrel{\gamma}{\rightarrow}  R, &     \gamma = 0.1 \\
    \end{aligned}
    \\
    \mathbf{x}_0 = [200 \: 1 \: 0]  \quad t_f = 150

In the above system, we start with 200 susceptible individuals, 1 infectious, and 0 recovered. The
deterministic results from `ODE45 <https://www.mathworks.com/help/matlab/ref/ode45.html>`_ are
compared to 20 stochastic trajectories simulated using the stochastic simulation algorithm by
:doc:`gillespie` (SSA) [1]_.

.. image:: /figures/SIRS_ODEvSSA.jpg
    :width: 50%
    :align: center

You can see that many of the SSA trajectories show no outbreak. This is because only one infectious
individual is in the initial state. The probability of the single infectious individual recovering
in the next time step is :math:`1/3 \left(\frac{1 \times 0.1}{200 \times 1 \times 0.001+1 \times
0.1}\right)`, while infecting one of 200 susceptible individual is :math:`2/3 \left(\frac{200 \times
1 \times 0.001}{200 \times 1 \times 0.001+1 \times 0.1}\right)`. At the same time, there are also
SSA trajectories that contain earlier and larger outbreaks (population of :math:`I`) compared to the
trajectory of :math:`I` from the deterministic simulation.

With a large number of SSA trajectories, you can obtain an accurate distribution of states in time.
For example, the distribution of recovered individuals :math:`R` at :math:`t = t_f(150)` using
:math:`10^5` SSA trajectories looks like the following:

.. image:: /figures/SIRS_tf_distrb_R.jpg
    :width: 50%
    :align: center

Such distributions can be used to obtain many useful insights into the system. The first mode in the
distribution (left peak) indicates that no large outbreak is observed almost half of the time by
:math:`t=150`. The second mode indicates the type of population immunity that may be observed at
:math:`t=150`. Looking at the same distribution in time can be used to study how the immunity
changes over time.

As the size of population increases, SSA trajectories start looking more similar to the ODE result
and exhibit less variability among themselves. When we change the initial population to
:math:`x_0 = [2000 \: 100 \: 0]`, we get the following result.

.. image:: /figures/SIRS_ODEvSSA_largePop.jpg
    :width: 50%
    :align: center

Intrinsic stochasticity may differ greatly from one model to another, depending on many factors,
such as reaction rates, number of non-linear reactions, connectivity among different compartments,
and population size. When a system contains compartments with a relatively large population where
stochasticity still matters, we can use :doc:`approximate-methods` to speed up the simulation.
Several popular :doc:`spatial-simulation-methods` are also supported in |CMS_s|, along with rare
event (:doc:`dwssa` and :doc:`sdwssa`) simulation methods.

.. rubric:: Footnotes

.. [1] `Gillespie, Daniel T. "Exact stochastic simulation of coupled chemical reactions." The Journal of Physical Chemistry 81.25 (1977): 2340-361. <http://pubs.acs.org/doi/abs/10.1021/j100540a008?journalCode=jpchax>`_