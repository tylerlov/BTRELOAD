using System.Collections.Generic;

namespace HohStudios.Common.Math
{
    /// <summary>
    /// This class performs a "weighted random" selection from the entries and weights given of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class WeightedRandom<T>
    {
        /// ____________________ FIELDS AND PROPERTIES ______________________________///

        private readonly System.Random _random = new System.Random(); // Cached random
        private readonly List<WeightedChoice> _choices = new List<WeightedChoice>(); // The list of possible choices and weights
        private int _totalWeight; // Total weight used in calculation

        /// <summary>
        /// Returns the total weight of all of the choices currently in the selector
        /// </summary>
        public int TotalWeight => _totalWeight;


        /// ____________________ WEIGHTED CHOICE STRUCTURE ______________________________///
        /// <summary>
        ///  Weighted Random struct to hold the possible choice and the total choice weight
        /// </summary>
        [System.Serializable]
        public struct WeightedChoice
        {
            public T Choice;
            public int ChoiceWeight;
            public int AdditiveWeight { get; private set; }

            /// <summary>
            /// Returns the current total additive weight at the time this choice was added
            /// </summary>
            /// <returns></returns>
            public void SetAdditiveWeight(int totalAdditiveWeight)
            {
                AdditiveWeight = totalAdditiveWeight;
            }
        }

        /// __________________________ UTILITY FUNCTIONS _______________________________///
        /// <summary>
        /// Adds a possible choice and weight to the WeightedRandom object into the random calculation
        /// </summary>
        /// <param name="choice"></param>
        /// <param name="weight"></param>
        public void AddChoice(T choice, int weight)
        {
            if (weight <= 0)
                return;

            _totalWeight += weight;

            var weightedChoice = new WeightedChoice() { Choice = choice, ChoiceWeight = weight };
            weightedChoice.SetAdditiveWeight(_totalWeight);
            _choices.Add(weightedChoice);
        }

        /// <summary>
        /// Updates a choice weight that already exists
        /// </summary>
        /// <param name="choiceToChange"></param>
        /// <param name="newWeight"></param>
        public void ChangeChoiceWeight(T choiceToChange, int newWeight)
        {
            if (newWeight <= 0)
                return;

            var tempChoices = new List<WeightedChoice>(_choices);

            ClearChoices();
            foreach (var choice in tempChoices)
                AddChoice(choice.Choice, choice.Equals(choiceToChange) ? newWeight : choice.ChoiceWeight);
        }

        /// <summary>
        /// Clears the weighted random choices list
        /// </summary>
        public void ClearChoices()
        {
            _choices.Clear();
            _totalWeight = 0;
        }

        /// <summary>
        /// Calculates the weighted random object of the choices given
        /// </summary>
        /// <returns></returns>
        public T Choose()
        {
            var random = _random.NextDouble() * _totalWeight;

            foreach (var entry in _choices)
            {
                if (entry.AdditiveWeight >= random)
                    return entry.Choice;
            }

            return default(T);
        }

        /// <summary>
        /// Returns the list of weighted choices
        /// </summary>
        /// <returns></returns>
        public List<WeightedChoice> GetChoices()
        {
            return _choices;
        }

    }
}
