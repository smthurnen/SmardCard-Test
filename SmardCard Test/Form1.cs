using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using Net.Pkcs11Interop.PDF;




namespace SmardCard_Test
{
    public partial class Form1 : Form
    {


        // Specify path to the unsigned PDF that will be created by this code
        string unsignedPdfPath = @"c:\temp\unsigned.pdf";
        // Specify path to the signed PDF that will be created by this code
        string signedPdfPath = @"c:\temp\signed.pdf";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            createPDF();
            signPDF(50,165,250,210);
        }

        private void createPDF()
        {


            // Specify path to the unsigned PDF that will be created by this code
            string unsignedPdfPath = @"c:\temp\unsigned.pdf";
            // Specify path to the signed PDF that will be created by this code
            string signedPdfPath = @"c:\temp\signed.pdf";
            // Create simple PDF document with iText
            using (Document document = new Document(PageSize.A4, 50, 50, 50, 50))
            {
                using (FileStream outputStream = new FileStream(unsignedPdfPath, FileMode.Create))
                {
                    using (PdfWriter pdfWriter = PdfWriter.GetInstance(document, outputStream))
                    {
                        document.Open();
                        document.Add(new Paragraph("Hello World!"));
                        document.Close();
                    }
                }
            }

        }

        private void signPDF(int llx, int lly, int urx, int ury)
        {
            // Do something interesting with unsigned PDF document
            FileInfo unsignedPdfInfo = new FileInfo(unsignedPdfPath);
            //Assert.IsTrue(unsignedPdfInfo.Length > 0);
            // Specify path to the unmanaged PCKS#11 library
            string libraryPath = @"C:\Windows\System32\cvP11.dll";
            // Specify serial number of the token that contains signing key. May be null if tokenLabel is specified.
            string tokenSerial = @"910e21b0da172e34";
            // Specify label of of the token that contains signing key. May be null if tokenSerial is specified
            string tokenLabel = @"SuisseID";
            // Specify PIN for the token
            string pin = "091011";
            // Specify label (value of CKA_LABEL attribute) of the private key used for signing. May be null if ckaId is specified.
            string ckaLabel = null;
            // Specify hex encoded string with identifier (value of CKA_ID attribute) of the private key used for signing. May be null if ckaLabel is specified.

            string ckaId = "6D808CE0BF9C368FB0AD28E24366F646BA0B3F67";
            // Specify hash algorihtm used for the signature creation
            HashAlgorithm hashAlgorithm = HashAlgorithm.SHA256;
            // Create instance of Pkcs11Signature class that allows iText to create PKCS#1 v1.5 RSA signature with the private key stored on PKCS#11 compatible device
            using (Pkcs11RsaSignature pkcs11RsaSignature = new Pkcs11RsaSignature(libraryPath, tokenSerial, tokenLabel, pin, ckaLabel, ckaId, HashAlgorithm.SHA256))
            {
                // When signing certificate is stored on the token it can be usually read with GetSigningCertificate() method
                byte[] signingCertificate = pkcs11RsaSignature.GetSigningCertificate();
                // All certificates stored on the token can be usually read with GetAllCertificates() method
                List<byte[]> otherCertificates = pkcs11RsaSignature.GetAllCertificates();
                // Build certification path for the signing certificate
                ICollection<Org.BouncyCastle.X509.X509Certificate> certPath = CertUtils.BuildCertPath(signingCertificate, otherCertificates);
                // Read unsigned PDF document
                using (PdfReader pdfReader = new PdfReader(unsignedPdfPath))
                {
                    // Create output stream for signed PDF document
                    using (FileStream outputStream = new FileStream(signedPdfPath, FileMode.Create))
                    {
                        // Create PdfStamper that applies extra content to the PDF document
                        using (PdfStamper pdfStamper = PdfStamper.CreateSignature(pdfReader, outputStream, '\0', Path.GetTempFileName(), true))
                        {
                            // Sign PDF document
                            PdfSignatureAppearance signatureAppearance = pdfStamper.SignatureAppearance;
                            signatureAppearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.GRAPHIC_AND_DESCRIPTION;
                            signatureAppearance.SignatureGraphic = iTextSharp.text.Image.GetInstance("logo_sign.png");
                            signatureAppearance.SetVisibleSignature(new iTextSharp.text.Rectangle((float)llx, (float)lly, (float)urx, (float)ury), 1, null);
                            MakeSignature.SignDetached(pdfStamper.SignatureAppearance, pkcs11RsaSignature, certPath, null, null, null, 0, CryptoStandard.CADES);
                            //MakeSignature.SignDetached(pdfStamper.SignatureAppearance, pkcs11RsaSignature, certPath, null, null, null, 0, CryptoStandard.CADES);
                        }
                    }
                }
            }
            // Do something interesting with the signed PDF document
            FileInfo signedPdfInfo = new FileInfo(signedPdfPath);
            //Assert.IsTrue(signedPdfInfo.Length > signedPdfPath.Length);

        }
    }
}
