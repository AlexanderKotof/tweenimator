namespace Tweenimator.Runtime.Bindings
{
    public interface IAnimationTrack<TValue>
    {
        KeyFrame<TValue>[] KeyFrames { get; }
        KeyFrame<TValue>[] GetKeyFrames();
    }
}
