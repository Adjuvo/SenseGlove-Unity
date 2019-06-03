using SenseGloveCs.Kinematics;
using UnityEngine;

public class SG_CalibrationExample : MonoBehaviour
{
    public SenseGlove_Object senseGlove;
    public KeyCode startCalibrationKey = KeyCode.LeftShift;
    public KeyCode cancelCalibrationKey = KeyCode.Escape;

    public KeyCode nextSolverKey = KeyCode.D, prevSolverKey = KeyCode.A;

    public TextMesh instrText, stepTxt, solvTxt;

    private CalibrateVariable variableToCalibrate;


    private int currentSolver = 0;
    private SenseGloveCs.Solver[] possibleSolvers = new SenseGloveCs.Solver[]
    {
        SenseGloveCs.Solver.Interpolate4Sensors,
        SenseGloveCs.Solver.InverseKinematics,
        SenseGloveCs.Solver.DistanceBased,
        SenseGloveCs.Solver.Interpolate2Sensors,
    };


    public string InstructionText
    {
        get { return (this.instrText != null ? this.instrText.text : "" ); }
        set { if (this.instrText != null) { this.instrText.text = value; } }
    }

    public string SolverText
    {
        get { return (this.solvTxt != null ? this.solvTxt.text : ""); }
        set { if (this.solvTxt != null) { this.solvTxt.text = value; } }
    }

    public string StepText
    {
        get { return (this.stepTxt != null ? this.stepTxt.text : ""); }
        set { if (this.stepTxt != null) { this.stepTxt.text = value; } }
    }

    /// <summary> Set the new Solver and also which variable to calibrate.  </summary>
    /// <param name="newSolver"></param>
    public void SetSolver(SenseGloveCs.Solver newSolver)
    {
        if (this.senseGlove != null)
        {
            this.senseGlove.CancelCalibration();
            this.senseGlove.solver = newSolver;
            switch (newSolver)
            {
                case SenseGloveCs.Solver.InverseKinematics:
                    variableToCalibrate = CalibrateVariable.FingerVariables; break;
                case SenseGloveCs.Solver.Interpolate4Sensors:
                    variableToCalibrate = CalibrateVariable.Interpolation_Flexion; break;
                case SenseGloveCs.Solver.Interpolate2Sensors:
                    variableToCalibrate = CalibrateVariable.Interpolation_Flexion; break;
            }
            this.SolverText = "Current Solver: " + this.senseGlove.solver.ToString() 
                + (this.CanCalibrate() ? "\r\nCalibrated using: " + this.variableToCalibrate.ToString() : "N\\A");
        }
    }


    public bool CanCalibrate()
    {
        return this.senseGlove != null && (this.senseGlove.solver == SenseGloveCs.Solver.InverseKinematics ||
            this.senseGlove.solver == SenseGloveCs.Solver.Interpolate2Sensors || this.senseGlove.solver == SenseGloveCs.Solver.Interpolate4Sensors);
    }


    private void SenseGlove_GloveLoaded(object source, System.EventArgs args)
    {
        this.InstructionText = "Calibration Example\r\nPress " + this.prevSolverKey.ToString() + "/" + this.nextSolverKey.ToString()
            + " to cycle solvers.\r\nPress " + this.startCalibrationKey + " to calibrate, R to reset.";
        this.SetSolver(SenseGloveCs.Solver.Interpolate4Sensors);
    }

    private void SenseGlove_CalibrationFinished(object source, GloveCalibrationArgs args)
    {
        this.InstructionText = "Calibration Example\r\nPress " + this.prevSolverKey.ToString() + "/" + this.nextSolverKey.ToString()
               + " to cycle solvers.\r\nPress " + this.startCalibrationKey + " to calibrate, R to reset.";
        this.StepText = "";
    }


    // Use this for initialization
    void Start ()
    {
        this.InstructionText = "Calibration Example\r\nWaiting for glove...";
        this.StepText = "";
        this.SolverText = "";
        if (this.senseGlove != null)
        {
            this.senseGlove.GloveLoaded += SenseGlove_GloveLoaded;
            this.senseGlove.CalibrationFinished += SenseGlove_CalibrationFinished;
        }
	}



    // Update is called once per frame
    void Update ()
    {
        if (this.senseGlove != null && this.senseGlove.GloveReady)
        {
            if (Input.GetKeyDown(this.nextSolverKey))
            {
                this.currentSolver++;
                if (this.currentSolver >= this.possibleSolvers.Length)
                    this.currentSolver = 0;
                this.SetSolver(this.possibleSolvers[this.currentSolver]);
            }
            else if (Input.GetKeyDown(this.prevSolverKey))
            {
                this.currentSolver--;
                if (this.currentSolver < 0)
                    this.currentSolver = this.possibleSolvers.Length - 1;
                this.SetSolver(this.possibleSolvers[this.currentSolver]);
            }


            if (Input.GetKeyDown(this.startCalibrationKey))
            {
                if (this.CanCalibrate())
                    this.senseGlove.StartCalibration(this.variableToCalibrate, CollectionMethod.SemiAutomatic);
                else
                    Debug.Log("Cannot start calibration with the " + this.senseGlove.solver.ToString() + " solver.");
            }
            else if (Input.GetKeyDown(this.cancelCalibrationKey))
            {
                this.StepText = "";
                this.senseGlove.CancelCalibration();
            }

            if (this.senseGlove.IsCalibrating)
            {
                SenseGlove_Data data = this.senseGlove.GloveData;
                this.StepText = "" + data.calibrationStep + " out of " + data.totalCalibrationSteps + " points collected.";
            }
        }
	}
}
