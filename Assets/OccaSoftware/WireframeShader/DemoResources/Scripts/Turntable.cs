using UnityEngine;

namespace OccaSoftware.Wireframe.Demo
{
	/// <summary>
	/// A small demo component. Used to rotate the object as if it were on a turntable.
	/// </summary>
	public class Turntable : MonoBehaviour
	{
		[SerializeField] float rotationsPerSecond = 0.25f;

		// Update is called once per frame
		void Update()
		{
			transform.Rotate(Vector3.up, rotationsPerSecond * 360f * Time.deltaTime);
		}
	}
}
