namespace UnityServer_selfmake_
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            richTextBox_notification = new RichTextBox();
            textBox_portTCP = new TextBox();
            label_portTCP = new Label();
            button_LISTEN = new Button();
            textBox_players = new TextBox();
            label_portUDP = new Label();
            textBox_portUDP = new TextBox();
            label_numofplayer = new Label();
            listBox_players = new ListBox();
            textBox_playertokick = new TextBox();
            button_kick = new Button();
            SuspendLayout();
            // 
            // richTextBox_notification
            // 
            richTextBox_notification.Location = new Point(12, 81);
            richTextBox_notification.Name = "richTextBox_notification";
            richTextBox_notification.Size = new Size(503, 319);
            richTextBox_notification.TabIndex = 0;
            richTextBox_notification.Text = "";
            // 
            // textBox_portTCP
            // 
            textBox_portTCP.Location = new Point(305, 12);
            textBox_portTCP.Name = "textBox_portTCP";
            textBox_portTCP.Size = new Size(100, 23);
            textBox_portTCP.TabIndex = 1;
            textBox_portTCP.Text = "10000";
            // 
            // label_portTCP
            // 
            label_portTCP.AutoSize = true;
            label_portTCP.Location = new Point(216, 15);
            label_portTCP.Name = "label_portTCP";
            label_portTCP.Size = new Size(58, 15);
            label_portTCP.TabIndex = 2;
            label_portTCP.Text = "PORT TCP";
            // 
            // button_LISTEN
            // 
            button_LISTEN.Location = new Point(440, 32);
            button_LISTEN.Name = "button_LISTEN";
            button_LISTEN.Size = new Size(75, 23);
            button_LISTEN.TabIndex = 3;
            button_LISTEN.Text = "LISTEN";
            button_LISTEN.UseVisualStyleBackColor = true;
            button_LISTEN.Click += button_LISTEN_Click;
            // 
            // textBox_players
            // 
            textBox_players.Location = new Point(649, 52);
            textBox_players.Name = "textBox_players";
            textBox_players.Size = new Size(44, 23);
            textBox_players.TabIndex = 4;
            // 
            // label_portUDP
            // 
            label_portUDP.AutoSize = true;
            label_portUDP.Location = new Point(213, 55);
            label_portUDP.Name = "label_portUDP";
            label_portUDP.Size = new Size(61, 15);
            label_portUDP.TabIndex = 5;
            label_portUDP.Text = "PORT UDP";
            // 
            // textBox_portUDP
            // 
            textBox_portUDP.Location = new Point(305, 52);
            textBox_portUDP.Name = "textBox_portUDP";
            textBox_portUDP.Size = new Size(100, 23);
            textBox_portUDP.TabIndex = 6;
            textBox_portUDP.Text = "10100";
            // 
            // label_numofplayer
            // 
            label_numofplayer.AutoSize = true;
            label_numofplayer.Location = new Point(599, 55);
            label_numofplayer.Name = "label_numofplayer";
            label_numofplayer.Size = new Size(44, 15);
            label_numofplayer.TabIndex = 7;
            label_numofplayer.Text = "players";
            // 
            // listBox_players
            // 
            listBox_players.FormattingEnabled = true;
            listBox_players.ItemHeight = 15;
            listBox_players.Location = new Point(521, 81);
            listBox_players.Name = "listBox_players";
            listBox_players.Size = new Size(258, 319);
            listBox_players.TabIndex = 8;
            listBox_players.SelectedIndexChanged += listBox_players_SelectedIndexChanged;
            // 
            // textBox_playertokick
            // 
            textBox_playertokick.Location = new Point(570, 405);
            textBox_playertokick.Name = "textBox_playertokick";
            textBox_playertokick.Size = new Size(100, 23);
            textBox_playertokick.TabIndex = 9;
            // 
            // button_kick
            // 
            button_kick.Location = new Point(686, 405);
            button_kick.Name = "button_kick";
            button_kick.Size = new Size(75, 23);
            button_kick.TabIndex = 10;
            button_kick.Text = "Kick";
            button_kick.UseVisualStyleBackColor = true;
            button_kick.Click += button_kick_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button_kick);
            Controls.Add(textBox_playertokick);
            Controls.Add(listBox_players);
            Controls.Add(label_numofplayer);
            Controls.Add(textBox_portUDP);
            Controls.Add(label_portUDP);
            Controls.Add(textBox_players);
            Controls.Add(button_LISTEN);
            Controls.Add(label_portTCP);
            Controls.Add(textBox_portTCP);
            Controls.Add(richTextBox_notification);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox richTextBox_notification;
        private TextBox textBox_portTCP;
        private Label label_portTCP;
        private Button button_LISTEN;
        private TextBox textBox_players;
        private Label label_portUDP;
        private TextBox textBox_portUDP;
        private Label label_numofplayer;
        private ListBox listBox_players;
        private TextBox textBox_playertokick;
        private Button button_kick;
    }
}
