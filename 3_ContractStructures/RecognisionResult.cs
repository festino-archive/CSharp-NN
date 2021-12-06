namespace Lab.Contract
{
    public class RecognisionResult
    {
        public RecognisionData[] Recognised { get; set; }

        public RecognisionResult(RecognisionData[] imageResult)
        {
            Recognised = imageResult;
        }
    }
}
