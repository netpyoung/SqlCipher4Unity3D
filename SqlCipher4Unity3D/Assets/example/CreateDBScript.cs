using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace example
{
    public class CreateDBScript : MonoBehaviour
    {
        [SerializeField]
        public Text DebugText;

        // Use this for initialization
        private void Start()
        {
            StartSync();
        }

        private void StartSync()
        {
            DataService ds = new DataService("tempDatabase.db");
            ds.CreateDB();

            IEnumerable<Person> people = ds.GetPersons();
            ToConsole(people);
            people = ds.GetPersonsNamedRoberto();
            ToConsole("Searching for Roberto ...");
            ToConsole(people);
        }

        private void ToConsole(IEnumerable<Person> people)
        {
            foreach (Person person in people) ToConsole(person.ToString());
        }

        private void ToConsole(string msg)
        {
            this.DebugText.text += Environment.NewLine + msg;
            Debug.Log(msg);
        }
    }
}
