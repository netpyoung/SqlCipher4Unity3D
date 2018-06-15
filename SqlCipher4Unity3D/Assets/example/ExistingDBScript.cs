using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace example
{

    public class ExistingDBScript : MonoBehaviour
    {
        public Text DebugText;

        // Use this for initialization
        private void Start()
        {
            DataService ds = new DataService("existing.db");
            ds.CreateDB();
            IEnumerable<Person> people = ds.GetPersons();
            ToConsole(people);

            people = ds.GetPersonsNamedRoberto();
            ToConsole("Searching for Roberto ...");
            ToConsole(people);

            ds.CreatePerson();
            ToConsole("New person has been created");
            Person p = ds.GetJohnny();
            ToConsole(p.ToString());
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