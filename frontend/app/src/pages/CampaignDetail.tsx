import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { campaignService } from '../services/api';
import { Campaign, CampaignSendLog, CampaignStatus } from '../types';
import { ChevronLeft, Edit, RefreshCw } from 'lucide-react';
import RichTextEditor from '../components/RichTextEditor';

const CampaignDetail: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [campaign, setCampaign] = useState<Campaign | null>(null);
  const [logs, setLogs] = useState<CampaignSendLog[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    if (!id) return;
    try {
      const [campData, logsData] = await Promise.all([
        campaignService.getById(id),
        campaignService.getLogs(id),
      ]);
      setCampaign(campData);
      setLogs(logsData);
    } catch (error) {
      console.error('Failed to load detail', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // Simple polling if sending to update progress
    const interval = setInterval(() => {
        if (campaign?.status === CampaignStatus.Sending) {
            loadData();
        }
    }, 5000);
    return () => clearInterval(interval);
  }, [id, campaign?.status]);

  if (loading || !campaign) return <div className="p-10 text-center">Loading details...</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
         <div className="flex items-center gap-3">
             <button
                onClick={() => navigate(-1)}
                className="p-2 hover:bg-gray-200 rounded-full transition-colors text-gray-500"
             >
                <ChevronLeft size={20} />
             </button>
             <div>
                <h1 className="text-2xl font-bold text-gray-900">{campaign.name}</h1>
                <p className="text-sm text-gray-500 flex items-center gap-2">
                    Status: <span className="font-medium text-gray-900">{campaign.status}</span>
                    {campaign.status === CampaignStatus.Sending && <RefreshCw size={12} className="animate-spin" />}
                </p>
             </div>
         </div>
         {campaign.status === CampaignStatus.Draft && (
             <Link
                to={`/campaigns/${campaign.id}/edit`}
                className="flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors shadow-sm"
             >
                <Edit size={16} />
                Edit Campaign
             </Link>
         )}
      </div>

      {/* Overview Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Total Recipients</div>
            <div className="text-2xl font-bold">{campaign.totalRecipients}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Sent</div>
            <div className="text-2xl font-bold text-green-600">{campaign.sentCount}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Failed</div>
            <div className="text-2xl font-bold text-red-600">{campaign.failedCount}</div>
        </div>
        <div className="bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
            <div className="text-sm text-gray-500 mb-1">Completion</div>
            <div className="text-2xl font-bold text-brand-blue">
                {campaign.totalRecipients > 0
                    ? Math.round(((campaign.sentCount + campaign.failedCount) / campaign.totalRecipients) * 100)
                    : 0
                }%
            </div>
        </div>
      </div>

      {/* Configuration & Body (read-only preview) */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm flex flex-col overflow-hidden">
        <div className="p-4 border-b border-gray-100 bg-gray-50 font-semibold text-gray-700">
          Campaign Preview
        </div>
        <div className="p-6 space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Campaign Name</label>
              <input
                type="text"
                value={campaign.name}
                disabled
                className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-gray-50 text-gray-700"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Subject</label>
              <input
                type="text"
                value={campaign.subject}
                disabled
                className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-gray-50 text-gray-700"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Sender Name</label>
              <input
                type="text"
                value={campaign.fromName}
                disabled
                className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-gray-50 text-gray-700"
              />
              <p className="text-xs text-gray-500 mt-1">Sender email comes from SMTP settings.</p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Created At</label>
              <input
                type="text"
                value={new Date(campaign.createdAt).toLocaleString()}
                disabled
                className="w-full px-3 py-2 border border-gray-300 rounded-lg bg-gray-50 text-gray-700"
              />
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold text-gray-800">Email Body</h3>
              {campaign.status === CampaignStatus.Sending && <RefreshCw size={14} className="animate-spin text-gray-400" />}
            </div>
            <RichTextEditor value={campaign.htmlBody} onChange={() => {}} disabled hideToolbar />
          </div>
        </div>
      </div>

      {/* Logs Panel */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden flex flex-col">
        <div className="p-4 border-b border-gray-100 font-semibold text-gray-800 flex justify-between items-center">
          <span>Send Logs</span>
          <button onClick={loadData} className="text-gray-400 hover:text-brand-blue transition-colors">
            <RefreshCw size={16} />
          </button>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-gray-600">
            <thead className="bg-gray-50 text-gray-500 uppercase font-medium text-xs">
              <tr>
                <th className="px-6 py-3">Recipient</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Message</th>
                <th className="px-6 py-3 text-right">Time</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {logs.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-gray-400 italic">
                    No logs available yet.
                  </td>
                </tr>
              ) : (
                logs.map((log) => (
                  <tr key={log.id} className="hover:bg-gray-50">
                    <td className="px-6 py-3 font-medium text-gray-900">{log.recipientEmail}</td>
                    <td className="px-6 py-3">
                      <span
                        className={`px-2 py-0.5 rounded text-xs font-medium ${
                          log.status === 'Sent' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                        }`}
                      >
                        {log.status}
                      </span>
                    </td>
                    <td className="px-6 py-3 text-red-500 text-xs max-w-xs truncate">{log.errorMessage || '-'}</td>
                    <td className="px-6 py-3 text-right text-gray-400 text-xs">
                      {log.sentAt ? new Date(log.sentAt).toLocaleTimeString() : '-'}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default CampaignDetail;
