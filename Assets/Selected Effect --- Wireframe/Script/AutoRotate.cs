using UnityEngine;

namespace SelectedEffectWireframe
{
	public class AutoRotate : MonoBehaviour
	{
		public float m_Speed = 30f;

		void Update()
		{
			float angle = Time.deltaTime * m_Speed;
			transform.Rotate(angle, angle, 0f);
		}
	}
}
