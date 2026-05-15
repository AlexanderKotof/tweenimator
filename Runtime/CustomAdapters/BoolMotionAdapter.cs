using LitMotion;

namespace Tweenimator.Runtime.CustomAdapters
{
    public readonly struct BoolMotionAdapter : IMotionAdapter<bool, NoOptions>
    {
        private const float ProgressThreashold = 0.99f;
        public bool Evaluate(ref bool startValue, ref bool endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return context.Progress >= ProgressThreashold ? endValue : startValue;
        }
    }
}
