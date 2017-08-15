=======================================
Introduction to compartmental modeling
=======================================


Compartmental modeling is widely used among epidemiologists to simulate disease dynamics. While deterministic compartmental models (ODEs and PDEs) are fast to simulate, there are many cases where stochastic modeling (CME and RDME based methods) is preferred. Stochastic framework provides *distributions* associated with characteristics of a process and rigorous procedures for inference. In addition when the population in any of the compartments is low, deterministic models do not provide accurate description of the system behavior. For example, consider the following simple SIR model. 


.. code-block:: ruby
  a = 1

.. code-block:: latex 
    
    \alpha

.. math::
    \alpha


.. code-block:: latex
    :caption: SIR

    S + I & \stackrel{\beta}{\rightarrow} 2I,&   0.005 \leq & \beta \leq 0.150 \\
    I & \stackrel{\gamma}{\rightarrow}  R, &   0.50 \leq & \gamma \leq 4.0 \\



.. math::

    \alpha

blah


