using FIMSpace.Basics;
using UnityEngine;


namespace FIMSpace.FOptimizing
{
    public class OptDemo_RandomizeFly : MonoBehaviour
    {
        public FBasic_FlyMovement flyMovement;

        public Vector2 rangeFromTo = new Vector2(20f, 100f);
        public Vector2 speedfromTo = new Vector2(0.5f, 1.5f);

        void Start()
        {
            flyMovement.RangeMul = Random.Range(rangeFromTo.x, rangeFromTo.y);
            //flyMovement.MainSpeed = Random.Range(speedfromTo.x, speedfromTo.y);
            flyMovement.MainSpeed = (flyMovement.RangeMul / 150) * Random.Range(speedfromTo.x, speedfromTo.y);

        }
    }
}