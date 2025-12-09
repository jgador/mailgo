import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { campaignService, recipientService } from '../services/api';
import { CampaignStatus, CreateCampaignRequest, SendNowRequest, SendTestRequest } from '../types';
import SmtpModal from '../components/SmtpModal';
import RichTextEditor from '../components/RichTextEditor';
import { Save, Send, Eye, Monitor, ChevronLeft } from 'lucide-react';

const CampaignEditor: React.FC = () => {
  const { id: routeId } = useParams();
  const navigate = useNavigate();
  const [campaignId, setCampaignId] = useState<string | null>(routeId ?? null);
  const isEditMode = !!campaignId;

  const [formData, setFormData] = useState<CreateCampaignRequest>({
    name: '',
    subject: '',
    fromName: '',
    htmlBody: '',
  });

  const [campaignStatus, setCampaignStatus] = useState<CampaignStatus>(CampaignStatus.Draft);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [preparingAction, setPreparingAction] = useState<'test' | 'send' | null>(null);
  
  // Modal State
  const [showTestModal, setShowTestModal] = useState(false);
  const [showSendModal, setShowSendModal] = useState(false);

  useEffect(() => {
    if (routeId && routeId !== campaignId) {
      setCampaignId(routeId);
    }
  }, [routeId, campaignId]);

  useEffect(() => {
    if (campaignId) {
      loadCampaign(campaignId);
    }
  }, [campaignId]);

  const loadCampaign = async (campaignId: string) => {
    setLoading(true);
    try {
      const data = await campaignService.getById(campaignId);
      setFormData({
        name: data.name,
        subject: data.subject,
        fromName: data.fromName,
        htmlBody: data.htmlBody,
      });
      setCampaignStatus(data.status);
    } catch (error) {
      console.error('Failed to load campaign', error);
      navigate('/', { replace: true });
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const ensureRequiredFields = () => {
    if (!formData.name || !formData.subject || !formData.fromName) {
      alert('Please fill in required fields (Name, Subject, Sender Name)');
      return false;
    }
    if (!formData.htmlBody.trim()) {
      alert('Please add an email body before saving or sending.');
      return false;
    }
    return true;
  };

  const ensureCampaignExists = async (): Promise<string | null> => {
    if (campaignId) {
      return campaignId;
    }
    if (!ensureRequiredFields()) {
      return null;
    }

    setSaving(true);
    try {
      const newCampaign = await campaignService.create(formData);
      setCampaignId(newCampaign.id);
      setCampaignStatus(newCampaign.status);
      return newCampaign.id;
    } catch (error) {
      console.error('Failed to create campaign', error);
      alert('Failed to create campaign.');
      return null;
    } finally {
      setSaving(false);
    }
  };

  const handleSave = async () => {
    if (!ensureRequiredFields()) {
      return;
    }

    setSaving(true);
    try {
      if (campaignId) {
        await campaignService.update(campaignId, formData);
      } else {
        await ensureCampaignExists();
      }
    } catch (error) {
      console.error('Failed to save', error);
      alert('Failed to save campaign.');
    } finally {
      setSaving(false);
    }
  };

  const ensureRecipientsExist = async (): Promise<boolean> => {
    try {
      const recipients = await recipientService.getAll(1, 1);
      if (recipients.totalItems === 0) {
        alert('Add at least one recipient before sending a campaign.');
        return false;
      }
      return true;
    } catch (error) {
      console.error('Failed to verify recipients', error);
      alert('Unable to verify recipients. Please try again.');
      return false;
    }
  };

  const handleSendTest = async (settings: SendTestRequest | SendNowRequest) => {
    const currentId = await ensureCampaignExists();
    if (!currentId || !('testEmail' in settings)) return;
    await campaignService.sendTest(currentId, settings);
    alert('Test email sent successfully!');
  };

  const handleSendNow = async (settings: SendTestRequest | SendNowRequest) => {
     const hasRecipients = await ensureRecipientsExist();
     if (!hasRecipients) return;
     const currentId = await ensureCampaignExists();
     if (!currentId) return;
     const payload: SendNowRequest = {
        smtpHost: settings.smtpHost,
        smtpPort: settings.smtpPort,
        smtpUsername: settings.smtpUsername,
        smtpPassword: settings.smtpPassword,
        smtpPasswordEncrypted: (settings as SendNowRequest).smtpPasswordEncrypted,
        smtpPasswordKeyId: (settings as SendNowRequest).smtpPasswordKeyId,
        encryption: settings.encryption,
        overrideFromName: settings.overrideFromName,
     };
     try {
       await campaignService.sendNow(currentId, payload);
       alert('Campaign started successfully!');
       navigate(`/campaigns/${currentId}`);
     } catch (error) {
       console.error('Failed to start campaign send', error);
       alert('Unable to start sending. Check SMTP settings and try again.');
     }
  };

  const handleOpenTestModal = async () => {
    if (isReadOnly) return;
    setPreparingAction('test');
    const ensuredId = await ensureCampaignExists();
    if (ensuredId) {
      setShowTestModal(true);
    }
    setPreparingAction(null);
  };

  const handleOpenSendModal = async () => {
    if (isReadOnly) return;
    setPreparingAction('send');
    const ensuredId = await ensureCampaignExists();
    if (ensuredId) {
      setShowSendModal(true);
    }
    setPreparingAction(null);
  };

  // If not draft, we shouldn't be here really, unless viewing code. 
  // But for now, let's assume we can only view if read-only logic was needed.
  // The PRD says "User can edit... while status is Draft".
  const isReadOnly = campaignStatus !== CampaignStatus.Draft;

  if (loading) return <div className="p-10 text-center">Loading editor...</div>;

  return (
    <div className="h-[calc(100vh-8rem)] flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
            <button onClick={() => navigate(-1)} className="p-2 hover:bg-gray-200 rounded-full transition-colors text-gray-500">
                <ChevronLeft size={20} />
            </button>
            <div>
                <h1 className="text-2xl font-bold text-gray-900">
                    {isEditMode ? 'Edit Campaign' : 'New Campaign'}
                </h1>
                <p className="text-sm text-gray-500">
                    {isReadOnly ? `Read Only (${campaignStatus})` : 'Compose your email'}
                </p>
            </div>
        </div>
        
        <div className="flex items-center gap-2">
            {!isReadOnly && (
                <button
                onClick={handleSave}
                disabled={saving}
                className="flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors shadow-sm disabled:opacity-50"
                >
                <Save size={18} />
                {saving ? 'Saving...' : 'Save Draft'}
                </button>
            )}
            
            {!isReadOnly && (
                <>
                <button
                    onClick={handleOpenTestModal}
                    className="flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors shadow-sm disabled:opacity-50"
                    disabled={preparingAction === 'test'}
                >
                    <Eye size={18} />
                    {preparingAction === 'test' ? 'Preparing...' : 'Send Test'}
                </button>
                <button
                    onClick={handleOpenSendModal}
                    className="flex items-center gap-2 bg-brand-blue text-white px-4 py-2 rounded-lg hover:bg-brand-blue-dark transition-colors shadow-sm disabled:opacity-50"
                    disabled={preparingAction === 'send'}
                >
                    <Send size={18} />
                    {preparingAction === 'send' ? 'Preparing...' : 'Send Now'}
                </button>
                </>
            )}
        </div>
      </div>

      {/* Single Column Layout */}
      <div className="flex-1 min-h-0">
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm flex flex-col overflow-hidden">
          <div className="p-4 border-b border-gray-100 bg-gray-50 font-semibold text-gray-700 flex items-center gap-2">
            Settings
          </div>
          <div className="p-6 space-y-4 overflow-y-auto flex-1">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Campaign Name</label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleChange}
                disabled={isReadOnly}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                placeholder="e.g. Monthly Newsletter"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Subject Line</label>
              <input
                type="text"
                name="subject"
                value={formData.subject}
                onChange={handleChange}
                disabled={isReadOnly}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                placeholder="Great news inside!"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Sender Name</label>
              <input
                type="text"
                name="fromName"
                value={formData.fromName}
                onChange={handleChange}
                disabled={isReadOnly}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-brand-blue outline-none disabled:bg-gray-100"
                placeholder="Mailgo"
              />
              <p className="text-xs text-gray-500 mt-1">Sender name can be overridden in SMTP settings.</p>
            </div>

            <div className="pt-2 space-y-2">
              <RichTextEditor
                value={formData.htmlBody}
                onChange={(html) => handleChange({ target: { name: 'htmlBody', value: html } } as any)}
                disabled={isReadOnly}
              />
            </div>

          </div>
        </div>
      </div>

      <SmtpModal
        isOpen={showTestModal}
        onClose={() => setShowTestModal(false)}
        onSubmit={handleSendTest}
        isTest={true}
        title="Send Test Email"
      />

      <SmtpModal
        isOpen={showSendModal}
        onClose={() => setShowSendModal(false)}
        onSubmit={handleSendNow}
        isTest={false}
        title="Send Campaign Now"
        summaryCampaignName={formData.name}
        summarySubject={formData.subject}
        summaryFromName={formData.fromName}
      />
    </div>
  );
};

export default CampaignEditor;
