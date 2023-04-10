namespace Mg.Util
{
    public class SampleFloat
    {

        public int count;       //样本数量
        public int idx = 0;     //最新样本 指针
        public float[] samples; //样本库
        public float sum;       //和
        public float ValNew;   //平均数
        public SampleFloat(int count,float initSample)
        {
            this.count = count;
            samples = new float[count];
            Reset(initSample);
        }
        public void AddSample(float newSample)
        {
            sum -= samples[idx];
            sum += newSample;
            ValNew = sum / count;

            samples[idx] = newSample;
            idx = (idx + 1) % count;
        }
        public void Reset(float bySample)
        {
            idx = 0;
            sum = bySample * count;
            ValNew = bySample;
            for (int i = 0; i < count; i++)
            {
                samples[i] = bySample;
            }
        }
    }
}


