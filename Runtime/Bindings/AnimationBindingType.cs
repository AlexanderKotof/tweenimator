namespace Tweenimator.Runtime.Bindings
{
    //List of supported animation bindings
    public enum AnimationBindingType
    {
        LocalPosition = 0,
        LocalRotation = 1,
        LocalScale = 2,

        AnchoredPosition = 10,
        SizeDelta = 11,

        GraphicColor = 100,
        PixelsPerUnit = 101,

        CanvasGroupAlpha = 200,

        GameObjectActivity = 1000,
        ComponentActivity = 1001,
    }
}
