using Recognision.DataStructures;
using System.Drawing;
using System.Threading.Tasks.Dataflow;

namespace Recognision
{
    internal interface IRecognisionTask
    {
        public ITargetBlock<RecognisionResult> OutputBlock { get; }
    }
    internal class FilesRecognisionTask : IRecognisionTask
    {
        public string[]? Filenames { get; }
        public ITargetBlock<RecognisionResult> OutputBlock { get; }

        public FilesRecognisionTask(string[]? filenames, CancellationToken token)
        {
            Filenames = filenames;
            OutputBlock = new BufferBlock<RecognisionResult>(new ExecutionDataflowBlockOptions
            {
                CancellationToken = token
            });
        }

        public FilesRecognisionTask(string[]? filenames, ITargetBlock<RecognisionResult> output)
        {
            Filenames = filenames;
            OutputBlock = output;
        }
    }
    internal class BitmapRecognisionTask : IRecognisionTask
    {
        public string Name { get; }
        public Bitmap Bitmap { get; }
        public ITargetBlock<RecognisionResult> OutputBlock { get; }

        public BitmapRecognisionTask(string name, Bitmap bitmap, CancellationToken token)
        {
            Name = name;
            Bitmap = bitmap;
            OutputBlock = new BufferBlock<RecognisionResult>(new ExecutionDataflowBlockOptions
            {
                CancellationToken = token
            });
        }

        public BitmapRecognisionTask(string name, Bitmap bitmap, ITargetBlock<RecognisionResult> output)
        {
            Name = name;
            Bitmap = bitmap;
            OutputBlock = output;
        }
    }
}
