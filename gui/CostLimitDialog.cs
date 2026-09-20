using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Harness98.Gui
{
    public sealed class CostLimitDialog : Form
    {
        public CostLimitDialog(double warningAmount, double sessionCost)
        {
            Text = "Session cost warning";
            ClientSize = new Size(410, 145);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            Label message = new Label();
            message.Location = new Point(14, 14);
            message.Size = new Size(382, 72);
            message.Text = "Session costs have exceeded $" +
                warningAmount.ToString("0.######", CultureInfo.InvariantCulture) +
                ".\r\n\r\nCurrent session cost: $" +
                sessionCost.ToString("0.######", CultureInfo.InvariantCulture) +
                "\r\nContinue running this request?";
            Controls.Add(message);

            Button continueButton = new Button();
            continueButton.Text = "Continue";
            continueButton.Location = new Point(234, 103);
            continueButton.Size = new Size(78, 28);
            continueButton.DialogResult = DialogResult.Yes;
            Controls.Add(continueButton);

            Button stopButton = new Button();
            stopButton.Text = "Stop";
            stopButton.Location = new Point(318, 103);
            stopButton.Size = new Size(78, 28);
            stopButton.DialogResult = DialogResult.No;
            Controls.Add(stopButton);

            AcceptButton = continueButton;
            CancelButton = stopButton;
        }
    }
}
