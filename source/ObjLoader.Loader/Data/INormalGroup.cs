namespace ObjLoader.Loader.Data
{
    public interface INormalGroup
    {
        Normal GetNormal(int i);
        void AddTexture(Normal normal);
    }
}