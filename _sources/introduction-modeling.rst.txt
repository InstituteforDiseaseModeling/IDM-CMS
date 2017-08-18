=======================================
Introduction to compartmental modeling
=======================================


Compartmental modeling is widely used among epidemiologists to simulate disease dynamics. While deterministic compartmental models (ODEs and PDEs) are fast to simulate, there are many cases where stochastic modeling (`CME <https://en.wikipedia.org/wiki/Master_equation>`_-based methods) is preferred. Stochastic framework provides *distributions* associated with characteristics of a process and rigorous procedures for inference. In addition, deterministic models do not provide an accurate description of the system when the population in any of the compartments is low. For example, consider the following simple SIR model. 

.. math::

    \begin{aligned}
    S + I & \stackrel{\beta}{\rightarrow} 2I,&   \beta = 0.001 \\
    I & \stackrel{\gamma}{\rightarrow}  R, &     \gamma = 0.1 \\
    \end{aligned}
    \\
    \mathbf{x}_0 = [200 \: 1 \: 0]  \quad t_f = 150

In the above system, we start with 200 susceptible individuals, 1 infectious and 0 recovered. Twenty stochastic trajectories are simulated using :doc:`Gillespie's stochastic simulation algorithm <Gillespie>` (SSA) [1]_ and its results are compared to that of `ODE45 <https://www.mathworks.com/help/matlab/ref/ode45.html>`_. 

.. image:: /figures/SIRS_ODEvSSA.jpg
    :width: 50%
    :align: center

We can see that many of the SSA trajectories show no outbreak. This is due to having only one infectious individual in the initial state. Probability of the single infectious individual recovering in the next time step is :math:`1/3 \left(\frac{1 \times 0.1}{200 \times 1 \times 0.001+1 \times 0.1}\right)` while infecting one of 200 susceptible individual is :math:`2/3 \left(\frac{200 \times 1 \times 0.001}{200 \times 1 \times 0.001+1 \times 0.1}\right)`. At the same time, we also see SSA trajectories that contain earlier and larger outbreak (population of :math:`I`) compared to the trajectory of :math:`I` from deterministic simulation.

With a large number of SSA trajectories, we can obtain an accurate distribution of states in time. For example, the distribution of recovered individuals :math:`R` at :math:`t = t_f(150)` using :math:`10^5` SSA trajectories looks like the following: 

.. image:: /figures/SIRS_tf_distrb_R.jpg
    :width: 50%
    :align: center

Such distribution can be used to obtain many useful insights into the system. The first mode in the distribution (left peak) indicates that no large outbreak is observed almost half of the time by :math:`t=150`. The second mode indicates the type of population immunity that may be observed at :math:`t=150`. Looking at the same distribution in time can be used to study how the immunity changes over time. 

As the size of population increases, SSA trajectories start looking more similiar to the ODE result and exhibit less variability among themselves. When we change the initial population to :math:`x_0 = [2000 \: 100 \: 0]`, we get the following result.

.. image:: /figures/SIRS_ODEvSSA_largePop.jpg
    :width: 50%
    :align: center

We note that intrinsic stochasticity may differ greatly from one model to another, depending on many factors, such as reaction rates, number of non-linear reactions, connectivity among different compartments, and population size. When a system contains compartments with a relatively large population where stochastcitity still matters, we can use an :doc:`approximate method <Approximate Methods>` to speed up the simulation. Several popular :doc:`spatial simulation methods <Spatial Simulation Methods>` are also supported in CMS, along with rare event (:doc:`dwSSA` and :doc:`sdwSSA`) and :doc:`exit time <ExitTimes>` simulation methods. 

.. rubric:: Footnotes
.. [1] `Gillespie, Daniel T. "Exact stochastic simulation of coupled chemical reactions." The Journal of Physical Chemistry 81.25 (1977): 2340-361. <http://pubs.acs.org/doi/abs/10.1021/j100540a008?journalCode=jpchax>`_