namespace StopTimetableGen.StopTimetableModel
{
    class ManualRemark : IRemark
    {
        public string Symbol { get; set; }

        public string Text { get; set; }

        public ManualRemark(string symbol, string text)
        {
            Symbol = symbol;
            Text = text;
        }
    }
}
