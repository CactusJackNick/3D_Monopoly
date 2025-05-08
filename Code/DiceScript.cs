// using UnityEngine;
// public class DiceScript : MonoBehaviour
// {
//     private Rigidbody _rb;
//     public int currentNumber = 0; // Store the current dice number
//
//     private void Awake()
//     {
//         _rb = GetComponent<Rigidbody>(); // Ensure Rigidbody is assigned
//     }
//     
//     public void Roll()
//     {
//         // Reset position, rotation, and physics
//         transform.position = new Vector3(transform.position.x, 2, transform.position.z);
//         transform.rotation = Random.rotation; // Randomize orientation
//         _rb.linearVelocity = Vector3.zero;
//         _rb.angularVelocity = Vector3.zero;
//
//         // Add random force and torque
//         float force = Random.Range(600, 700);
//         _rb.AddForce(Vector3.up * force);
//         _rb.AddTorque(Random.insideUnitSphere * (force * 2));
//         
//     }
//
//     public bool IsStationary()
//     {
//         if (_rb is null)
//         {
//             _rb = GetComponent<Rigidbody>();
//         }
//         return _rb.linearVelocity.magnitude < 0.1f && _rb.angularVelocity.magnitude < 0.1f;
//     }
//
//     public void SetNumber(int number)
//     {
//         currentNumber = number;
//     }
//
//     public int GetNumber()
//     {
//         return currentNumber;
//     }
// } PREVIOUS SCRIPT TO SIMPLY LAUNCH AND ROLL DICE IN AIR

using System.Collections;
using UnityEngine;

namespace Code
{
    public class DiceScript : MonoBehaviour
    {
        private Animator animator;
        public int currentNumber;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetNumber(int number)
        {
            currentNumber = number;
        }
    
        public int GetNumber()
        {
            return currentNumber;
        }

        public bool IsStationary()
        {
            // Check if the dice animation has finished playing
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
        }
        public void RollToSpecificNumber(int targetNumber)
        {
            if (!DiceManager.Instance.rolling) return; // 🚀 Only animate when actually rolling
            currentNumber = targetNumber;
            // Wait a short time before playing animation
            StartCoroutine(PlayAnimationAfterJump(targetNumber));
        }

         private IEnumerator PlayAnimationAfterJump(int targetNumber)
         {
             yield return new WaitForSeconds(0.5f); // Wait before playing animation
        
              //Play the correct landing animation
             string animationName = gameObject.name.Contains("Dice1") ? 
                 $"Dice_Land_{targetNumber}" : $"Dice2_Land_{targetNumber}";
             animator.Play(animationName);
        }

    }
}
