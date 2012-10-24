using System;

namespace DynamicProxy
{
	/// <summary>
	/// </summary>
	public class TestBed
	{
		/// <summary>
		/// </summary>
		[STAThread]
		static void Main( string[] args ) {
            ITest test = (ITest)SecurityProxy.NewInstance(new TestImpl());
            
            //test.TestFunctionOne();
            //Console.WriteLine(test.TestFunctionTwo(1, 2));
            test.TestFunctionThree(1, 2);
		}
	}

    public interface ITest {
        //void TestFunctionOne();
        //int TestFunctionTwo(int a, int b);
        void TestFunctionThree(int a, int b);
    }

    public class TestImpl : ITest {
        //public void TestFunctionOne()
        //{
        //    Console.WriteLine("In TestImpl.TestFunctionOne()");
        //}

        //public int TestFunctionTwo(int a, int b)
        //{
        //    Console.WriteLine("In TestImpl.TestFunctionTwo(Object a, Object b)");
        //    return a + b;
        //}

        public void TestFunctionThree(int a, int b)
        {
            Console.WriteLine("In TestImpl.TestFunctionThree(Object a, Object b)");
            Console.WriteLine(a + b);
        }
    }
}
