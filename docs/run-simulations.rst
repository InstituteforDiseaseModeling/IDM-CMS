====================
Running a simulation
====================

To run a |CMS_s| simulation, do the following:

#.  Open a Command Prompt window and navigate to the directory that contains the configuration and
    model files.
#.  Enter a command like the one illustrated below, substituting the full path to the executable and file
    names for your simulation::

    ../compartments.exe -c <config_file.cfg> -m <model_file.emodl>

    Where <config_file.cfg> and <model_file.emodl> are names of your configuration and model files,
    respectively. If you run a simulation from a different directory, you must include the full path to
    these files.

#.  |CMS_s| will display status information, including any errors that occur, while running the
    simulation.

#.  By default, the output file trajectories.csv will be created in the current directory. You
    can specify additional output files in the configuration file.


.. note::

    You can add the path to the executable to your **PATH** environment variable so you do not need to
    type the full path to compartments.exe.

    To set **PATH** for the current Command Prompt window only, use ``set``. For example, ``set
    PATH=%PATH%;c:\cms\``.

    To permanently modify **PATH**, use ``setx``. For example, ``setx PATH "%PATH%;c:\cms"``. You
    must then open a new Command Prompt window.
