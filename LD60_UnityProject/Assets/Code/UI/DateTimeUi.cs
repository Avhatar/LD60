using System;
using LD60;
using TMPro;
using UnityEngine;

public class DateTimeUi : MonoBehaviour
{
    [SerializeField]
    private TMP_Text dateTimeText;

    private int _shownYear = -1;
    private int _shownMonth = -1;
    private int _shownDay = -1;

    private void Start()
    {
        if (dateTimeText == null)
        {
            Debug.LogError($"{nameof(DateTimeUi)}: {nameof(dateTimeText)} is null in {nameof(Start)} on '{name}'. " +
                           "Required to draw the date the run has reached. " +
                           "Fix: assign a TextMeshPro text object to the Date Time Text slot on this object.", this);
            enabled = false;
            return;
        }

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        DateTime date = Core.CurrentDate;

        if (date.Year == _shownYear && date.Month == _shownMonth && date.Day == _shownDay)
            return;

        _shownYear = date.Year;
        _shownMonth = date.Month;
        _shownDay = date.Day;
        dateTimeText.text = string.Format("{0:00}.{1:00}.{2:0000}", _shownDay, _shownMonth, _shownYear);
    }
}
