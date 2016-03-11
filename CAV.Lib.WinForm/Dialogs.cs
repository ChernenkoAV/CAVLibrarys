using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Cav.WinForm;

namespace Cav
{
    /// <summary>
    /// Заготовки диалогов
    /// </summary>
    public static class Dialogs
    {
        #region Forms, Owner
        /// <summary>
        /// Вопрос с ответами Ok|Cancel (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static bool QuestionOKCancelF(Control Owner, String Text, String Caption = null)
        {
            if (Caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    Caption = Application.OpenForms[0].Text;

            if (Caption.IsNullOrWhiteSpace())
            {
                var frm = Owner as Form;
                if (frm != null)
                    Caption = frm.Text;
            }

            return System.Windows.Forms.MessageBox.Show(Owner, Text, Caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        /// <summary>
        /// Вопрос с ответами Yes|No|Cancel (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static Boolean? QuestionYesNoCancelF(Control Owner, String Text, String Caption = null)
        {
            if (Caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    Caption = Application.OpenForms[0].Text;

            if (Caption.IsNullOrWhiteSpace())
            {
                var frm = Owner as Form;
                if (frm != null)
                    Caption = frm.Text;
            }

            DialogResult dr = System.Windows.Forms.MessageBox.Show(Owner, Text, Caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return dr == DialogResult.Cancel ? (Boolean?)null : dr == DialogResult.Yes;
        }

        /// <summary>
        /// Информационное (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static void InformationF(Control Owner, String Text, String Caption = null)
        {
            if (Caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    Caption = Application.OpenForms[0].Text;

            if (Caption.IsNullOrWhiteSpace())
            {
                var frm = Owner as Form;
                if (frm != null)
                    Caption = frm.Text;
            }

            System.Windows.Forms.MessageBox.Show(Owner, Text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Ошибка (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static void ErrorF(Control Owner, String Text, String Caption = null)
        {
            if (Caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    Caption = Application.OpenForms[0].Text;

            if (Caption.IsNullOrWhiteSpace())
            {
                var frm = Owner as Form;
                if (frm != null)
                    Caption = frm.Text;
            }

            System.Windows.Forms.MessageBox.Show(Owner, Text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Forms
        /// <summary>
        /// Вопрос с ответами Ok|Cancel (Forms)
        /// </summary>
        /// <returns></returns>
        public static Boolean QuestionOKCancelF(String Text, String Caption)
        {
            return System.Windows.Forms.MessageBox.Show(Text, Caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        /// <summary>
        /// Вопрос с ответами Yes|No|Cancel (Forms)
        /// </summary>
        /// <returns></returns>
        public static Boolean? QuestionYesNoCancelF(String Text, String Caption)
        {
            DialogResult dr = System.Windows.Forms.MessageBox.Show(Text, Caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return dr == DialogResult.Cancel ? (Boolean?)null : dr == DialogResult.Yes;
        }

        /// <summary>
        /// Информационное (Forms)
        /// </summary>
        /// <returns></returns>
        public static void InformationF(String Text, String Caption)
        {
            System.Windows.Forms.MessageBox.Show(Text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Ошибка (Forms)
        /// </summary>
        /// <returns></returns>
        public static void ErrorF(String Text, String Caption)
        {
            System.Windows.Forms.MessageBox.Show(Text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion


        /// <summary>
        /// Диалог выбора папки(директории)
        /// </summary>
        /// <param name="Owner">Контрол, к котрому показывается диалог.</param>
        /// <param name="Description">Описание</param>
        /// <param name="RootFolder">Корневая папка</param>
        /// <param name="SelectedPath">Выбранная папка</param>
        /// <param name="ShowNewFolderButton">Показывать кнопку создания новой папки</param>
        /// <returns>Выбранная папка (null - отмена выбора)</returns>
        public static String FolderBrowser(
            Control Owner = null,
            String Description = null,
            Environment.SpecialFolder? RootFolder = null,
            String SelectedPath = null,
            Boolean ShowNewFolderButton = false)
        {
            FolderBrowserDialog dg = new FolderBrowserDialog();
            if (!Description.IsNullOrWhiteSpace())
                dg.Description = Description;
            if (RootFolder.HasValue)
                dg.RootFolder = RootFolder.Value;
            if (!SelectedPath.IsNullOrWhiteSpace())
                dg.SelectedPath = SelectedPath;

            DialogResult dr;
            if (Owner != null)
                dr = dg.ShowDialog(Owner);
            else
                dr = dg.ShowDialog();

            if (dr != DialogResult.OK)
                return null;
            return dg.SelectedPath;
        }

        /// <summary>
        /// Диалог выбора файлов
        /// </summary>
        /// <param name="Owner">Контрол, к котрому показывается диалог.</param>
        /// <param name="Title">Задает заголовок диалогового окна файла.</param>
        /// <param name="Multiselect">Задает значение, указывающее, можно ли в диалоговом окне выбирать несколько файлов.</param>
        /// <param name="SupportMultiDottedExtensions">Задает условие, поддерживает ли диалоговое окно отображение файлов, которые содержат несколько расширений имени файла.</param>
        /// <param name="InitialDirectory">Задает начальную папку, отображенную диалоговым окном файла.</param>
        /// <param name="FilterIndex">Задает индекс фильтра, выбранного в настоящий момент в диалоговом окне файла.</param>
        /// <param name="Filter">Задает текущую строку фильтра имен файлов, которая определяет варианты, доступные в поле "Файлы типа" диалогового окна.</param>         
        /// <param name="FileName">Задает строку, содержащую имя файла, выбранное в диалоговом окне файла.</param>
        /// <param name="DefaultExt">Задает расширение имени файла по умолчанию</param>
        /// <param name="RestoreDirectory"></param>
        /// <param name="CheckPathExists">Задает значение, указывающее, отображает ли диалоговое окно предупреждение, если пользователь указывает несуществующий путь</param>
        /// <param name="CheckFileExists">Задает значение, указывающее, отображается ли в диалоговом окне предупреждение, если пользователь указывает несуществующее имя файла.</param>
        /// <param name="AddExtension">Задает значение, определяющее, добавляет ли автоматически диалоговое окно расширение к имени файла, если пользователь опускает данное расширение.</param>
        public static List<String> FileBrowser(
            Control Owner = null,
            String Title = null,
            Boolean Multiselect = false,
            Boolean SupportMultiDottedExtensions = true,
            String InitialDirectory = null,
            int FilterIndex = 0,
            String Filter = null,
            String FileName = null,
            String DefaultExt = null,
            Boolean RestoreDirectory = false,
            Boolean CheckPathExists = true,
            Boolean CheckFileExists = true,
            Boolean AddExtension = true)
        {
            List<String> res = new List<string>();

            OpenFileDialog fd = new OpenFileDialog()
                {
                    Title = Title,
                    Multiselect = Multiselect,
                    SupportMultiDottedExtensions = SupportMultiDottedExtensions,
                    InitialDirectory = InitialDirectory,
                    FilterIndex = FilterIndex,
                    Filter = Filter,
                    FileName = FileName,
                    DefaultExt = DefaultExt,
                    RestoreDirectory = RestoreDirectory,
                    CheckPathExists = CheckPathExists,
                    CheckFileExists = CheckFileExists,
                    AddExtension = AddExtension,
                };

            DialogResult dr;
            if (Owner != null)
                dr = fd.ShowDialog(Owner);
            else
                dr = fd.ShowDialog();

            if (dr == DialogResult.OK)
                res.AddRange(fd.FileNames);
            return res;
        }


        /// <summary>
        /// Диалоговое окно сохранения файла
        /// </summary>
        /// <param name="Owner">Контрол, к котрому показывается диалог.</param>
        /// <param name="Title">Задает заголовок диалогового окна файла.</param>
        /// <param name="SupportMultiDottedExtensions">Задает условие, поддерживает ли диалоговое окно отображение файлов, которые содержат несколько расширений имени файла.</param>
        /// <param name="InitialDirectory">Задает начальную папку, отображенную диалоговым окном файла.</param>
        /// <param name="FilterIndex">Задает индекс фильтра, выбранного в настоящий момент в диалоговом окне файла.</param>
        /// <param name="Filter">Задает текущую строку фильтра имен файлов, которая определяет варианты, доступные в поле "Файлы типа" диалогового окна.</param>         
        /// <param name="FileName">Задает строку, содержащую имя файла, выбранное в диалоговом окне файла.</param>
        /// <param name="DefaultExt">Задает расширение имени файла по умолчанию</param>
        /// <param name="RestoreDirectory"></param>
        /// <param name="CheckPathExists">Задает значение, указывающее, отображает ли диалоговое окно предупреждение, если пользователь указывает несуществующий путь</param>
        /// <param name="CheckFileExists">Задает значение, указывающее, отображается ли в диалоговом окне предупреждение, если пользователь указывает несуществующее имя файла.</param>
        /// <param name="AddExtension">Задает значение, определяющее, добавляет ли автоматически диалоговое окно расширение к имени файла, если пользователь опускает данное расширение.</param>
        /// <param name="OverwritePrompt">Задает значение, показывающее, будет ли диалоговое окно Save As выводить предупреждение, если файл с указанным именем уже существует.</param>
        /// <returns>Строка, содержащую имя файла, выбранное в диалоговом окне файла. null, если в диалоге ничего не выбрали</returns>
        public static String SaveFile(
            Control Owner = null,
            String Title = null,
            String Filter = null,
            String FileName = null,
            Boolean OverwritePrompt = true,
            Boolean SupportMultiDottedExtensions = false,
            String InitialDirectory = null,
            int FilterIndex = 0,
            String DefaultExt = null,
            Boolean RestoreDirectory = false,
            Boolean CheckPathExists = true,
            Boolean CheckFileExists = false,
            Boolean AddExtension = true)
        {
            String file = null;

            SaveFileDialog fd = new SaveFileDialog()
            {
                Title = Title,
                OverwritePrompt = OverwritePrompt,
                SupportMultiDottedExtensions = SupportMultiDottedExtensions,
                InitialDirectory = InitialDirectory,
                FilterIndex = FilterIndex,
                Filter = Filter,
                FileName = FileName,
                DefaultExt = DefaultExt,
                RestoreDirectory = RestoreDirectory,
                CheckPathExists = CheckPathExists,
                CheckFileExists = CheckFileExists,
                AddExtension = AddExtension,
            };

            DialogResult dr;

            if (Owner != null)
                dr = fd.ShowDialog(Owner);
            else
                dr = fd.ShowDialog();


            if (dr == DialogResult.OK)
                file = fd.FileName;

            return file;
        }

        /// <summary>
        /// Окно ввода пользователем строкового значения
        /// </summary>
        /// <param name="Owner">Владелец окна</param>
        /// <param name="Title">Текс в Caption</param>
        /// <param name="DescriptionText">Текст описания</param>
        /// <param name="DefautText">Значение, которое будет в окне ввода при отображении диалога(текст по умолчанию)</param>
        /// <param name="MaxLength">Длина вводимой строки</param>
        /// <returns>null - если нажали Отмена, значение. если нажали Ok</returns>
        public static String InputBox(
            Control Owner = null,
            String Title = null,
            String DescriptionText = "",
            String DefautText = "",
            int? MaxLength = null)
        {

            var ibf = new InputBoxForm();
            ibf.Text = Title;
            ibf.lbDescriptionText.Text = DescriptionText;
            ibf.tbInputText.Text = DefautText;
            if (MaxLength.HasValue)
                ibf.tbInputText.MaxLength = MaxLength.Value;

            String res = null;

            DialogResult dr;

            if (Owner != null)
                dr = ibf.ShowDialog(Owner);
            else
                dr = ibf.ShowDialog();

            if (dr == DialogResult.OK)
                res = ibf.tbInputText.Text;

            return res;
        }


    }
}
