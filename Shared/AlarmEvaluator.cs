namespace Shared;

public static class AlarmEvaluator
{
    public static int Evaluate(double value, double threshold1, double threshold2, double threshold3)
    {
        if (value >= threshold3)
        {
            return 3;
        }

        if (value >= threshold2)
        {
            return 2;
        }

        if (value >= threshold1)
        {
            return 1;
        }

        return 0;
    }
}
