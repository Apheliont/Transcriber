using System;
using System.ComponentModel;


namespace Transcriber.Core.Models
{
    public class TranscriptionModel
    {
        private string _text;
        private string _partial;

        public TranscriptionModel()
        {
            _text = string.Empty;
            _partial = string.Empty;
        }

        public string Partial
        {
            get => _partial;
            set
            {
                _partial = value;
            }
        }

        public string Text
        {
            get
            {
                if (String.IsNullOrEmpty(Partial))
                {
                    return _text;
                }
                else
                {
                    if (String.IsNullOrEmpty(_text))
                    {
                        return Partial;
                    }
                    return _text + " " + Partial;
                }
            }

            set
            {
                if (String.IsNullOrEmpty(_text))
                {
                    _text = value.Trim();
                } else
                {
                    _text += " " + value;
                }
                Partial = string.Empty;
            }
        }


        public void Clear()
        {
            _text = string.Empty;
            _partial = string.Empty;
        }
    }
}
