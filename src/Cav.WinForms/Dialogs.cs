using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Cav.WinForms;

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
        public static bool QuestionOKCancelF(
            Control owner,
            String text,
            String caption = null)
        {
            if (caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    caption = Application.OpenForms[0].Text;

            if (caption.IsNullOrWhiteSpace())
            {
                if (owner is Form frm)
                    caption = frm.Text;
            }

            return MessageBox.Show(owner, text, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        /// <summary>
        /// Вопрос с ответами Yes|No|Cancel (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static Boolean? QuestionYesNoCancelF(
            Control owner,
            String text,
            String caption = null)
        {
            if (caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    caption = Application.OpenForms[0].Text;

            if (caption.IsNullOrWhiteSpace())
            {
                if (owner is Form frm)
                    caption = frm.Text;
            }

            var dr = MessageBox.Show(owner, text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return dr == DialogResult.Cancel ? (Boolean?)null : dr == DialogResult.Yes;
        }

        /// <summary>
        /// Информационное (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static void InformationF(
            Control owner,
            String text,
            String caption = null)
        {
            if (caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    caption = Application.OpenForms[0].Text;

            if (caption.IsNullOrWhiteSpace())
            {
                if (owner is Form frm)
                    caption = frm.Text;
            }

            MessageBox.Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Ошибка (Forms)(Owner)
        /// </summary>
        /// <returns></returns>
        public static void ErrorF(
            Control owner,
            String text,
            String caption = null)
        {
            if (caption.IsNullOrWhiteSpace())
                if (Application.OpenForms.Count > 0)
                    caption = Application.OpenForms[0].Text;

            if (caption.IsNullOrWhiteSpace())
            {
                if (owner is Form frm)
                    caption = frm.Text;
            }

            MessageBox.Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Forms
        /// <summary>
        /// Вопрос с ответами Ok|Cancel (Forms)
        /// </summary>
        /// <returns></returns>
        public static Boolean QuestionOKCancelF(
            String text,
            String caption) =>
            MessageBox.Show(text, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;

        /// <summary>
        /// Вопрос с ответами Yes|No|Cancel (Forms)
        /// </summary>
        /// <returns></returns>
        public static Boolean? QuestionYesNoCancelF(
            String text,
            String caption)
        {
            var dr = MessageBox.Show(text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            return dr == DialogResult.Cancel ? (Boolean?)null : dr == DialogResult.Yes;
        }

        /// <summary>
        /// Информационное (Forms)
        /// </summary>
        /// <returns></returns>
        public static void InformationF(
            String text,
            String caption) =>
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

        /// <summary>
        /// Ошибка (Forms)
        /// </summary>
        /// <returns></returns>
        public static void ErrorF(
            String text,
            String caption) =>
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);

        #endregion

        /// <summary>
        /// Диалог выбора папки(директории)
        /// </summary>
        /// <param name="owner">Контрол, к котрому показывается диалог.</param>
        /// <param name="description">Описание</param>
        /// <param name="rootFolder">Корневая папка</param>
        /// <param name="selectedPath">Выбранная папка</param>
        /// <param name="showNewFolderButton">Показывать кнопку создания новой папки</param>
        /// <returns>Выбранная папка (null - отмена выбора)</returns>
        public static String FolderBrowser(
            Control owner = null,
            String description = null,
            Environment.SpecialFolder? rootFolder = null,
            String selectedPath = null,
            Boolean showNewFolderButton = false)
        {
            using (var dg = new FolderBrowserDialog())
            {
                if (!description.IsNullOrWhiteSpace())
                    dg.Description = description;
                if (rootFolder.HasValue)
                    dg.RootFolder = rootFolder.Value;
                if (!selectedPath.IsNullOrWhiteSpace())
                    dg.SelectedPath = selectedPath;

                dg.ShowNewFolderButton = showNewFolderButton;

                var dr = owner != null ? dg.ShowDialog(owner) : dg.ShowDialog();

                return dr != DialogResult.OK ? null : dg.SelectedPath;
            }
        }

        /// <summary>
        /// Диалог выбора файлов
        /// </summary>
        /// <param name="owner">Контрол, к котрому показывается диалог.</param>
        /// <param name="title">Задает заголовок диалогового окна файла.</param>
        /// <param name="multiselect">Задает значение, указывающее, можно ли в диалоговом окне выбирать несколько файлов.</param>
        /// <param name="supportMultiDottedExtensions">Задает условие, поддерживает ли диалоговое окно отображение файлов, которые содержат несколько расширений имени файла.</param>
        /// <param name="initialDirectory">Задает начальную папку, отображенную диалоговым окном файла.</param>
        /// <param name="filterIndex">Задает индекс фильтра, выбранного в настоящий момент в диалоговом окне файла.</param>
        /// <param name="filter">Задает текущую строку фильтра имен файлов, которая определяет варианты, доступные в поле "Файлы типа" диалогового окна.(Text Files (*.txt)|*.txt|All Files (*.*)|*.*)</param>
        /// <param name="fileName">Задает строку, содержащую имя файла, выбранное в диалоговом окне файла.</param>
        /// <param name="defaultExt">Задает расширение имени файла по умолчанию</param>
        /// <param name="restoreDirectory"></param>
        /// <param name="checkPathExists">Задает значение, указывающее, отображает ли диалоговое окно предупреждение, если пользователь указывает несуществующий путь</param>
        /// <param name="checkFileExists">Задает значение, указывающее, отображается ли в диалоговом окне предупреждение, если пользователь указывает несуществующее имя файла.</param>
        /// <param name="addExtension">Задает значение, определяющее, добавляет ли автоматически диалоговое окно расширение к имени файла, если пользователь опускает данное расширение.</param>
        public static List<String> FileBrowser(
            Control owner = null,
            String title = null,
            Boolean multiselect = false,
            Boolean supportMultiDottedExtensions = true,
            String initialDirectory = null,
            int filterIndex = 0,
            String filter = null,
            String fileName = null,
            String defaultExt = null,
            Boolean restoreDirectory = false,
            Boolean checkPathExists = true,
            Boolean checkFileExists = true,
            Boolean addExtension = true)
        {
            var res = new List<string>();

            using (var fd = new OpenFileDialog()
            {
                Title = title,
                Multiselect = multiselect,
                SupportMultiDottedExtensions = supportMultiDottedExtensions,
                InitialDirectory = initialDirectory,
                FilterIndex = filterIndex,
                Filter = filter,
                FileName = fileName,
                DefaultExt = defaultExt,
                RestoreDirectory = restoreDirectory,
                CheckPathExists = checkPathExists,
                CheckFileExists = checkFileExists,
                AddExtension = addExtension,
            })
            {
                var dr = owner != null ? fd.ShowDialog(owner) : fd.ShowDialog();

                if (dr == DialogResult.OK)
                    res.AddRange(fd.FileNames);
                return res;
            }
        }

        /// <summary>
        /// Диалоговое окно сохранения файла
        /// </summary>
        /// <param name="owner">Контрол, к котрому показывается диалог.</param>
        /// <param name="title">Задает заголовок диалогового окна файла.</param>
        /// <param name="supportMultiDottedExtensions">Задает условие, поддерживает ли диалоговое окно отображение файлов, которые содержат несколько расширений имени файла.</param>
        /// <param name="initialDirectory">Задает начальную папку, отображенную диалоговым окном файла.</param>
        /// <param name="filterIndex">Задает индекс фильтра, выбранного в настоящий момент в диалоговом окне файла.</param>
        /// <param name="filter">Задает текущую строку фильтра имен файлов, которая определяет варианты, доступные в поле "Файлы типа" диалогового окна.(Text Files (*.txt)|*.txt|All Files (*.*)|*.*)</param>
        /// <param name="fileName">Задает строку, содержащую имя файла, выбранное в диалоговом окне файла.</param>
        /// <param name="defaultExt">Задает расширение имени файла по умолчанию</param>
        /// <param name="restoreDirectory"></param>
        /// <param name="checkPathExists">Задает значение, указывающее, отображает ли диалоговое окно предупреждение, если пользователь указывает несуществующий путь</param>
        /// <param name="checkFileExists">Задает значение, указывающее, отображается ли в диалоговом окне предупреждение, если пользователь указывает несуществующее имя файла.</param>
        /// <param name="addExtension">Задает значение, определяющее, добавляет ли автоматически диалоговое окно расширение к имени файла, если пользователь опускает данное расширение.</param>
        /// <param name="overwritePrompt">Задает значение, показывающее, будет ли диалоговое окно Save As выводить предупреждение, если файл с указанным именем уже существует.</param>
        /// <returns>Строка, содержащую имя файла, выбранное в диалоговом окне файла. null, если в диалоге ничего не выбрали</returns>
        public static String SaveFile(
            Control owner = null,
            String title = null,
            String filter = null,
            String fileName = null,
            Boolean overwritePrompt = true,
            Boolean supportMultiDottedExtensions = false,
            String initialDirectory = null,
            int filterIndex = 0,
            String defaultExt = null,
            Boolean restoreDirectory = false,
            Boolean checkPathExists = true,
            Boolean checkFileExists = false,
            Boolean addExtension = true)
        {
            String file = null;

            using (var fd = new SaveFileDialog()
            {
                Title = title,
                OverwritePrompt = overwritePrompt,
                SupportMultiDottedExtensions = supportMultiDottedExtensions,
                InitialDirectory = initialDirectory,
                FilterIndex = filterIndex,
                Filter = filter,
                FileName = fileName,
                DefaultExt = defaultExt,
                RestoreDirectory = restoreDirectory,
                CheckPathExists = checkPathExists,
                CheckFileExists = checkFileExists,
                AddExtension = addExtension,
            })
            {

                var dr = owner != null ? fd.ShowDialog(owner) : fd.ShowDialog();

                if (dr == DialogResult.OK)
                    file = fd.FileName;

                return file;
            }
        }

        /// <summary>
        /// Окно ввода пользователем строкового значения
        /// </summary>
        /// <param name="owner">Владелец окна</param>
        /// <param name="title">Текс в Caption</param>
        /// <param name="descriptionText">Текст описания</param>
        /// <param name="defautText">Значение, которое будет в окне ввода при отображении диалога(текст по умолчанию)</param>
        /// <param name="maxLength">Длина вводимой строки</param>
        /// <returns>null - если нажали Отмена, значение. если нажали Ok</returns>
        public static String InputBox(
            Control owner = null,
            String title = null,
            String descriptionText = null,
            String defautText = null,
            int? maxLength = null)
        {
            descriptionText = descriptionText ?? String.Empty;
            defautText = defautText ?? String.Empty;

            using (var ibf = new InputBoxForm())
            {
                ibf.Text = title;
                ibf.lbDescriptionText.Text = descriptionText;
                ibf.tbInputText.Text = defautText;
                if (maxLength.HasValue)
                    ibf.tbInputText.MaxLength = maxLength.Value;

                ibf.CorrrectHeightForm();

                String res = null;

                var dr = owner != null ? ibf.ShowDialog(owner) : ibf.ShowDialog();

                if (dr == DialogResult.OK)
                    res = ibf.tbInputText.Text;

                return res;
            }
        }
    }
}
